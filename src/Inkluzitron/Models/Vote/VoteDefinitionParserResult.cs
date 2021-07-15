namespace Inkluzitron.Models.Vote
{
    public struct VoteDefinitionParserResult
    {
        public VoteDefinition Definition { get; set; }
        public string ProblemDescription { get; set; }
        public bool Success => ProblemDescription == null;
    }
}
