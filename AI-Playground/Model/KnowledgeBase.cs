namespace Model
{
    public static class KnowledgeBase
    {
        public static Fact[] Facts =
        [
            new Fact
            {
                FactId = "1",
                FactName = "Company-Cloud-Computing",
                FactDescription = "Leading provider of cloud computing services and solutions.",
                FactVector = new float[1536]
            },
            new Fact
            {
                FactId = "2",
                FactName = "Company-Artificial-Intelligence",
                FactDescription = "Innovative technology company specializing in artificial intelligence and machine learning.",
                FactVector = new float[1536]
            },
            new Fact
            {
                FactId = "3",
                FactName = "Company-Ecommerce",
                FactDescription = "Global e-commerce platform connecting buyers and sellers worldwide.",
                FactVector = new float[1536]
            },
            new Fact
            {
                FactId = "4",
                FactName = "Company-Telecommunication",
                FactDescription = "Major telecommunication company providing mobile, internet, and digital services across the region.",
                FactVector = new float[1536]
            },
            new Fact
            {
                FactId = "5",
                FactName = "Company-Health-Care",
                FactDescription = "Leading health care company offering innovative medical solutions and patient care services.",
                FactVector = new float[1536]
            }
        ];
    }
}
