namespace SaveBackups.Core;

/// <summary>A step on disk either worked, with its value, or failed with a reason for the log and the player.</summary>
public abstract record Result<T>
{
    private Result()
    {
    }

    public sealed record Done(T Value) : Result<T>;

    public sealed record Failed(string Why) : Result<T>;
}

/// <summary>For steps that give nothing back.</summary>
public readonly record struct Unit;

public static class Result
{
    public static Result<T> Done<T>(T value) => new Result<T>.Done(value);

    public static Result<T> Failed<T>(string why) => new Result<T>.Failed(why);
}
