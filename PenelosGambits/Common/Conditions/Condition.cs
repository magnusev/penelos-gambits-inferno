public interface Condition
{
    bool IsMet(Environment environment);

    void Consume();
}