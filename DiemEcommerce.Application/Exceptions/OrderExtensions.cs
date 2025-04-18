using System.Text.RegularExpressions;

namespace DiemEcommerce.Application.Exceptions;

public static class OrderExtensions
{
    public static Guid GuidParser(string guidString)
    {
        if (guidString.Length != 32)
        {
            throw new FormatException("Invalid GUID format. GUID should be 32 characters long.");
        }

        // Insert hyphens to match the Guid format (8-4-4-4-12)
        string formattedOrderId = $"{guidString.Substring(0, 8)}-{guidString.Substring(8, 4)}-{guidString.Substring(12, 4)}-{guidString.Substring(16, 4)}-{guidString.Substring(20)}";

        // Parse the string back into a Guid
        return Guid.Parse(formattedOrderId);
    }
    
    public static Guid TakeOrderIdFromContent(string content)
    {
        // Ensure that the orderId is extracted using the regex
        string orderId = "";
        string note = content;

        // Adjusted regex to ensure it matches a 32-character alphanumeric string after "QR"
        Match match = Regex.Match(note, @"QR\s+([a-fA-F0-9]{32})");

        if (match.Success)
        {
            orderId = match.Groups[1].Value;
        }
        else
        {
            throw new FormatException("OrderId not found or invalid format.");
        }

        return GuidParser(orderId);
    }
    
}