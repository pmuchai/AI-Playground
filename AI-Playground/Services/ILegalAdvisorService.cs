namespace Services
{
    public interface ILegalAdvisorService
    {
        /// <summary>
        /// Produce a short, neutral, responsible legal answer for the supplied user prompt.
        /// </summary>
        string Advise(string userPrompt);
    }
}
