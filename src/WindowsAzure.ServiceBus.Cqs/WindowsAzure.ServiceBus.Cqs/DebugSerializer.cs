using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WindowsAzure.ServiceBus.Cqs
{
    class DebugSerializer
    {
        private static JsonSerializerSettings _settings = new JsonSerializerSettings()
        {
            Formatting = Formatting.None,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
        };

        public static string Serialize(object msg)
        {
            return JsonConvert.SerializeObject(msg);
        }
    }
}
