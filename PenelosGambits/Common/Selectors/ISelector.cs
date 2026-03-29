public interface ISelector
{
    Unit Select(Environment environment);

    void Log(Environment environment);
}