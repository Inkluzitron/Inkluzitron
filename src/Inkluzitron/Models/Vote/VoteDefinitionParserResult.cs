namespace Inkluzitron.Models.Vote
{
    public struct VoteDefinitionParserResult
    {
        public bool Success => ProblemDescription == null;
        public VoteDefinition Definition { get; set; }
        public string ProblemDescription { get; set; }
        public string Notice { get; set; }
    }
}
