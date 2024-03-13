using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Reflection;
using Newtonsoft.Json;
using SalesBotApi.Models;

namespace SalesBotApi.Controllers
{
    [Route("api/[controller]")] 
    [ApiController]
    public class ConfigController : Controller
    {

        private readonly IOptions<MySettings> _mySettings;
        private readonly IOptions<MyConnectionStrings> _myConnectionStrings;

        public ConfigController(
            IOptions<MySettings> _mySettings,
            IOptions<MyConnectionStrings> _myConnectionStrings
        )
        {
            this._mySettings = _mySettings;
            this._myConnectionStrings = _myConnectionStrings;
        }

        // GET: api/config
        [HttpGet]
        [JwtAuthorize]
        public ActionResult<string> GetConfig()
        {
            JwtPayload userData = HttpContext.Items["UserData"] as JwtPayload;
            string role = userData.role;
            if(role != "root") {
                return Unauthorized();
            }
            
            MySettings mySettings = _mySettings.Value;
            MyConnectionStrings myConnectionStrings = _myConnectionStrings.Value;
            ModifyStringProperties(myConnectionStrings);
            var resp = new
            {
                mySettings,
                myConnectionStrings
            };
            string configStr = JsonConvert.SerializeObject(resp, Formatting.Indented);
            return configStr;
        }

        public void ModifyStringProperties<T>(T model)
        {
            // Get all properties of the model
            PropertyInfo[] properties = typeof(T).GetProperties();

            foreach (PropertyInfo property in properties)
            {
                // Check if the property is of type string
                if (property.PropertyType == typeof(string))
                {
                    // Get the current value of the property
                    string currentValue = (string)property.GetValue(model);

                    // Modify the value if it's not null
                    if (currentValue != null)
                    {
                        string modifiedValue = ReplaceMiddleWithEllipsis(currentValue);
                        property.SetValue(model, modifiedValue);
                    }
                }
            }
        }

        private string ReplaceMiddleWithEllipsis(string input)
        {
            if (string.IsNullOrEmpty(input) || input.Length <= 4)
            {
                return input;
            }

            return input.Substring(0, 2) + "..." + input.Substring(input.Length - 2);
        }
    }

}
