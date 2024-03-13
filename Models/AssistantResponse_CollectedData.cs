using System.Collections.Generic;

namespace SalesBotApi.Models

{
    public class AssistantResponse_CollectedData_DecedentInformation {
        public string full_legal_name { get; set;}
        public string place_of_death { get; set;}
        public string date_of_death { get; set;}
        public string city_of_death { get; set;}
        public string county_of_death { get; set;}
    }
    public class AssistantResponse_CollectedData_AffiantInformation {
        public string first_name { get; set;}
        public string last_name { get; set;}
        public string email_address { get; set;}
        public string phone_number { get; set;}
        public string relationship_to_decedent { get; set;}
        public bool affiant_is_successor { get; set;}
    }
    public class AssistantResponse_CollectedData_EstateInformation {
        public float total_value { get; set;}
        public List<string> itemized_assets { get; set; }
    }
    public class AssistantResponse_CollectedData {
        public AssistantResponse_CollectedData_DecedentInformation decedent_information { get; set;}
        public AssistantResponse_CollectedData_AffiantInformation affiant_information { get; set;}
        public AssistantResponse_CollectedData_EstateInformation estate_information { get; set;}
        public string description_of_assets { get; set;}
    }

    public class AssistantResponse {
        public string assistant_response { get; set;}
        public AssistantResponse_CollectedData collected_data { get; set;}
    }
}
