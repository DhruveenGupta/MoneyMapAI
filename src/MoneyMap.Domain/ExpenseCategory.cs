namespace MoneyMap.Domain;

// Kept short on purpose - a first-time budgeter doesn't need a full chart of accounts.
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
    // Rough need-vs-want split, only used for the beginner education copy.
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
