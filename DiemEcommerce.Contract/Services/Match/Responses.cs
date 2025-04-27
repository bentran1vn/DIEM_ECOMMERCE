namespace DiemEcommerce.Contract.Services.Match;

public class Responses
{
    public class MatchResponse
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public IEnumerable<MatchMedia> CoverImages { get; set; }
        public Guid CategoryId { get; set; } 
        public string CategoryName { get; set; } 
        public Guid FactoryId { get; set; }
        public string FactoryName { get; set; }
        public string FactoryAddress { get; set; }
        public string FactoryPhoneNumber { get; set; }
    }

    public class MatchMedia
    {
        public Guid Id { get; set; }
        public string Url { get; set; }
    }
    
    public class MatchDetailResponse : MatchResponse
    {
        public string FactoryEmail { get; set; }
        public string FactoryWebsite { get; set; }
        public string FactoryDescription { get; set; }
        public string FactoryTaxCode { get; set; }
        public string FactoryBankAccount { get; set; }
        public string FactoryBankName { get; set; }
        public string FactoryLogo { get; set; }
        public ICollection<Contract.Services.Feedback.Responses.FeedbackResponse> Feedbacks { get; set; }
    }
}