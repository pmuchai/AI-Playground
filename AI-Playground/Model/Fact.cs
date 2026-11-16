namespace Model
{
    public class Fact
    {
        public string FactId { get; set; } = string.Empty;
        public string FactName { get; set; } = string.Empty;
        public string FactDescription { get; set; } = string.Empty;
        public IReadOnlyList<float> FactVector { get; set; } = new List<float>();
    };

}
