namespace ClubRegistration.Mvc.Models
{
    public class ClubCard
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string Teacher { get; set; }
        public string Schedule { get; set; }
        public int AgeFrom { get; set; }
        public int AgeTo { get; set; }
        public int FreePlaces { get; set; }
        public decimal Price { get; set; }
    }
}
