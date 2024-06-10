namespace ProbeSampler.Presentation.Base
{
    public interface INestedMessageSender
    {
        ISBMessageSender? MessageSender { get; set; }
    }
}
