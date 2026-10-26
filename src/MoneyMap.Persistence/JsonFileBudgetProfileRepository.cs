using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MoneyMap.Application;

namespace MoneyMap.Persistence;

public sealed class JsonFileBudgetProfileRepositoryOptions
{
    public required string DataDirectory { get; init; }

    public string FileName { get; init; } = "moneymap-profile.dat";

    // On Windows this encrypts the file with DPAPI, scoped to the current user account. No
    // effect on other platforms - the file is just plain JSON there.
    public bool EnableEncryptionAtRest { get; init; } = true;
}

// Single-user JSON file on disk. No multi-tenancy, no network access - this is meant for one
// person running the app on their own machine, not a shared deployment.
public sealed class JsonFileBudgetProfileRepository : IBudgetProfileRepository
{
    private static readonly byte[] EncryptedHeader = "MMENC1\n"u8.ToArray();
    private static readonly byte[] PlainHeader = "MMPLN1\n"u8.ToArray();
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    private readonly JsonFileBudgetProfileRepositoryOptions _options;
    private readonly ILogger<JsonFileBudgetProfileRepository> _logger;
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private readonly string _filePath;

    public JsonFileBudgetProfileRepository(JsonFileBudgetProfileRepositoryOptions options, ILogger<JsonFileBudgetProfileRepository> logger)
    {
        _options = options;
        _logger = logger;
        Directory.CreateDirectory(_options.DataDirectory);
        _filePath = Path.Combine(_options.DataDirectory, _options.FileName);
    }

    public async Task<BudgetProfile?> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(_filePath))
            {
                return null;
            }

            var raw = await File.ReadAllBytesAsync(_filePath, cancellationToken).ConfigureAwait(false);

            try
            {
                var json = Decode(raw);
                var dto = JsonSerializer.Deserialize<BudgetProfileDto>(json, SerializerOptions)
                    ?? throw new JsonException("Profile file deserialized to null.");
                return BudgetProfileMapper.ToDomain(dto);
            }
            catch (Exception ex) when (ex is JsonException or CryptographicException or ArgumentException or FormatException)
            {
                QuarantineCorruptFile(ex);
                return null;
            }
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task SaveAsync(BudgetProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var dto = BudgetProfileMapper.ToDto(profile);
        var json = JsonSerializer.Serialize(dto, SerializerOptions);
        var payload = Encode(json);

        await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var tempPath = _filePath + ".tmp";
            await File.WriteAllBytesAsync(tempPath, payload, cancellationToken).ConfigureAwait(false);
            // Write to a temp file then swap, so a crash mid-write can't leave a half-written profile.
            File.Move(tempPath, _filePath, overwrite: true);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }

            foreach (var backup in Directory.EnumerateFiles(_options.DataDirectory, _options.FileName + ".corrupted-*.bak"))
            {
                File.Delete(backup);
            }
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private byte[] Encode(string json)
    {
        var jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);

        if (_options.EnableEncryptionAtRest && OperatingSystem.IsWindows())
        {
#pragma warning disable CA1416 // guarded by OperatingSystem.IsWindows()
            var protectedBytes = System.Security.Cryptography.ProtectedData.Protect(jsonBytes, optionalEntropy: null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
#pragma warning restore CA1416
            return [.. EncryptedHeader, .. protectedBytes];
        }

        return [.. PlainHeader, .. jsonBytes];
    }

    private string Decode(byte[] raw)
    {
        if (StartsWith(raw, EncryptedHeader))
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new CryptographicException("Profile file was encrypted on Windows but is being read on a non-Windows platform.");
            }

            var payload = raw[EncryptedHeader.Length..];
#pragma warning disable CA1416
            var jsonBytes = System.Security.Cryptography.ProtectedData.Unprotect(payload, optionalEntropy: null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
#pragma warning restore CA1416
            return System.Text.Encoding.UTF8.GetString(jsonBytes);
        }

        if (StartsWith(raw, PlainHeader))
        {
            return System.Text.Encoding.UTF8.GetString(raw[PlainHeader.Length..]);
        }

        throw new FormatException("Profile file is missing a recognized header.");
    }

    private static bool StartsWith(byte[] data, byte[] prefix)
        => data.Length >= prefix.Length && data.AsSpan(0, prefix.Length).SequenceEqual(prefix);

    private void QuarantineCorruptFile(Exception reason)
    {
        try
        {
            var backupPath = $"{_filePath}.corrupted-{DateTimeOffset.UtcNow.Ticks}.bak";
            File.Move(_filePath, backupPath, overwrite: false);
            _logger.LogWarning(reason, "Stored budget profile at {FilePath} could not be read and was quarantined to {BackupPath}. Starting from a blank profile instead of discarding the file.", _filePath, backupPath);
        }
        catch (Exception moveEx)
        {
            _logger.LogError(moveEx, "Failed to quarantine unreadable profile file at {FilePath}.", _filePath);
        }
    }
}
