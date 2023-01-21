namespace logging_benchmark;

internal class MyValue
{
    private readonly string _value; 
    public MyValue(string inner)
    {
        _value = inner;
    }

    public override string ToString()
    {
        // this will be called once MyValue is logged
        return _value[3..];
    }
}
