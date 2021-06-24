namespace Inkluzitron.Models
{
    public class KisPrestigeResult
    {
        public string ErrorMessage { get; set; }
        public int Prestige { get; set; }

        public bool IsOk => string.IsNullOrEmpty(ErrorMessage);
    }
}
