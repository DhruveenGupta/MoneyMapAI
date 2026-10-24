namespace MoneyMap.Domain;

/// <summary>
/// Fixed set of budget categories. Kept deliberately small and generic (rather than a full
/// chart-of-accounts) so a first-time budgeter is not overwhelmed, while still covering the
/// common expense types called out in the product spec.
/// </summary>
public enum ExpenseCategory
{
    Housing,
    Utilities,
    Groceries,
    SchoolMeals,
    Transportation,
    Subscriptions,
    Debt,
    Healthcare,
    Discretionary,
    Other,
}

public static class ExpenseCategoryInfo
{
    /// <summary>
    /// Whether a category is generally a "need" (vs. "want"). Used only for beginner-friendly
    /// education copy; it never gates or judges what a user enters.
    /// </summary>
    public static bool IsTypicallyNeed(ExpenseCategory category) => category switch
    {
        ExpenseCategory.Housing => true,
        ExpenseCategory.Utilities => true,
        ExpenseCategory.Groceries => true,
        ExpenseCategory.SchoolMeals => true,
        ExpenseCategory.Transportation => true,
        ExpenseCategory.Debt => true,
        ExpenseCategory.Healthcare => true,
        ExpenseCategory.Subscriptions => false,
        ExpenseCategory.Discretionary => false,
        ExpenseCategory.Other => false,
        _ => false,
    };

    public static string DisplayName(ExpenseCategory category) => category switch
    {
        ExpenseCategory.SchoolMeals => "School Meals",
        _ => category.ToString(),
    };
}
