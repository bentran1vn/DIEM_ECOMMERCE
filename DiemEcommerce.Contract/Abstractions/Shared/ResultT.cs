using System.Text.Json.Serialization;

namespace DiemEcommerce.Contract.Abstractions.Shared;

public class Result<TValue> : Result
{
    private readonly TValue? _value;
    protected internal Result(TValue? value ,bool isSuccess, Error error) : base(isSuccess, error)
    {
        _value = value;
    }

    public TValue Value => IsSuccess ? _value! : throw new InvalidOperationException("The value of failure result can not be access");
    
    [JsonConstructor]
    public Result(TValue value) : this(value, true, Error.None) { }
    
    public static implicit operator Result<TValue>(TValue? value) => Create(value)!;
}
