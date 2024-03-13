using System.Collections.Generic;
using System.Linq;

namespace SalesBotApi.Models

{
    public class Conversation
    {
        public string id { get; set; }
        public string user_id { get; set; }
        public string company_id { get; set; }
        public long _ts { get; set; }


        public static AssistantResponse_CollectedData AggregateAssistantResponses(List<Message> messages)
        {
            AssistantResponse_CollectedData aggregated_collected_data = new AssistantResponse_CollectedData
            {
                decedent_information = new AssistantResponse_CollectedData_DecedentInformation(),
                affiant_information = new AssistantResponse_CollectedData_AffiantInformation(),
                estate_information = new AssistantResponse_CollectedData_EstateInformation
                {
                    itemized_assets = new List<string>()
                }
            };

            foreach (var message in messages)
            {
                if (message.assistant_response != null && message.assistant_response.collected_data != null)
                {
                    MergeAssistantResponse(aggregated_collected_data, message.assistant_response.collected_data);
                }
            }

            return aggregated_collected_data;
        }

        public static void MergeAssistantResponse(AssistantResponse_CollectedData target, AssistantResponse_CollectedData source)
        {
            if (source.decedent_information?.full_legal_name    != null) target.decedent_information.full_legal_name    = source.decedent_information.full_legal_name;
            if (source.decedent_information?.place_of_death     != null) target.decedent_information.place_of_death     = source.decedent_information.place_of_death;
            if (source.decedent_information?.date_of_death      != null) target.decedent_information.date_of_death      = source.decedent_information.date_of_death;
            if (source.decedent_information?.city_of_death      != null) target.decedent_information.city_of_death      = source.decedent_information.city_of_death;
            if (source.decedent_information?.county_of_death    != null) target.decedent_information.county_of_death    = source.decedent_information.county_of_death;

            if (source.affiant_information?.first_name                  != null) target.affiant_information.first_name                  = source.affiant_information.first_name;
            if (source.affiant_information?.last_name                   != null) target.affiant_information.last_name                   = source.affiant_information.last_name;
            if (source.affiant_information?.email_address               != null) target.affiant_information.email_address               = source.affiant_information.email_address;
            if (source.affiant_information?.phone_number                != null) target.affiant_information.phone_number                = source.affiant_information.phone_number;
            if (source.affiant_information?.relationship_to_decedent    != null) target.affiant_information.relationship_to_decedent    = source.affiant_information.relationship_to_decedent;
            
            // Bool needs to be handled differently
            target.affiant_information.affiant_is_successor = target.affiant_information.affiant_is_successor || (source.affiant_information!=null && source.affiant_information.affiant_is_successor);

            // Number needs to be handled differently
            if (source.estate_information?.total_value != null && 
                source.estate_information?.total_value>0 && 
                source.estate_information?.total_value > target.estate_information.total_value
            ) target.estate_information.total_value = source.estate_information.total_value;

            if (target.estate_information.itemized_assets == null) target.estate_information.itemized_assets = new List<string>();
            var assetsSet = new HashSet<string>(target.estate_information.itemized_assets);
            if (source.estate_information?.itemized_assets != null)
            {
                foreach (var asset in source.estate_information.itemized_assets) assetsSet.Add(asset);
                target.estate_information.itemized_assets = assetsSet.ToList();
            }
        }
    }
}