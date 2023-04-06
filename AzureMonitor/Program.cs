using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Linq;
using Azure.ResourceManager.Resources;
using Newtonsoft.Json;
using Azure.Monitor.Query.Models;
using AzureMonitor.Models;
using Azure.Monitor.Query;

namespace AzureMonitor
{
    internal class Program
    {
        static HttpClient client = new HttpClient();

        static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
               .AddEnvironmentVariables()
               .Build();

            // account 정보는 az account show 로 확인 가능.
            // clientid, clientSecret 는 Azure AD 에서 앱 등록 필요.
            var tenantId = config["tenantId"] ?? "";
            var clientId = config["clientId"] ?? "";
            var clientSecret = config["clientSecret"] ?? "";
            var subscriptionId = config["subscriptionid"] ?? "";

            //Access Token 발급
            var token = await GetAccessToken(tenantId, clientId, clientSecret);

            client.DefaultRequestHeaders.Authorization =  new AuthenticationHeaderValue("Bearer", token);

            var resourceGroupName = "demo-iothub-1206";

            var allResources = await GetAllResourcesAsync(client, subscriptionId, resourceGroupName);

            var iothubResourceId = string.Empty;

            //테스트로 결과 출력
            allResources.value.ToList().ForEach(r =>
            {
                Console.WriteLine("------------------------------------------------------------");
                Console.WriteLine($"id: {r.id}");
                Console.WriteLine($"name: {r.name}");
                Console.WriteLine($"type: {r.type}");
                Console.WriteLine($"location: {r.location}");
                Console.WriteLine($"tags: {r.tags}");
                Console.WriteLine($"kind: {r.kind}");
                Console.WriteLine($"managedBy: {r.managedBy}");
                Console.WriteLine($"sku: {r.sku}");
                Console.WriteLine($"plan: {r.plan}");
                Console.WriteLine($"properties: {r.properties}");
                Console.WriteLine($"identity: {r.identity}");
                Console.WriteLine($"zones: {r.zones}");
                Console.WriteLine($"extendedLocation: {r.extendedLocation}");
                Console.WriteLine($"systemData: {r.systemData}");

                // 테스트용 iot hub 인 경우
                if(r.type == "Microsoft.Devices/IotHubs")
                {
                    iothubResourceId = r.id;
                }
            });

            //https://learn.microsoft.com/en-us/rest/api/monitor/metrics/list?tabs=HTTP#response
            //Metrics - List
            var metricslist = await GetMetricsList(client, iothubResourceId);

            Console.WriteLine(metricslist);


            //옵션을 이용하는 방법은 metric namespace 등을 수동으로 지정해 줘야 하므로 가급적 Azure.Monitor.Query 패키지를 이용하시기 바랍니다.
            //string timespan = DateTime.UtcNow.AddHours(-24).ToString("yyyy-MM-ddTHH:mm:ssZ") + "/" + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            //string interval = System.Xml.XmlConvert.ToString(TimeSpan.FromMinutes(5)); //PT30M
            //var metricslistwithoptions = await GetMetricsListWithOptions(client, iothubResourceId, timespan: timespan, interval: interval, metricnames:"Connected devices", aggregation:"Average", metricnamespace: "Microsoft.Devices/IotHubs");
            //Console.WriteLine(metricslistwithoptions);


            var metricsClient = new MetricsQueryClient(new ClientSecretCredential(tenantId: tenantId, clientId: clientId, clientSecret: clientSecret));

            //metric namespace
            var metricNamespace = metricsClient.GetMetricNamespaces(iothubResourceId);
            var metricNamespaceName = metricNamespace.FirstOrDefault().FullyQualifiedName;
            

            var metricDefinition = metricsClient.GetMetricDefinitions(iothubResourceId, metricNamespaceName);

            // 쿼리 할 수 있는 메트릭 이름 조회
            metricDefinition.ToList().ForEach(x =>
            {
                Console.WriteLine(x.Name);
            });

            MetricsQueryOptions options = new MetricsQueryOptions();
            options.Granularity = TimeSpan.FromMinutes(1);
            options.TimeRange = new QueryTimeRange(TimeSpan.FromMinutes(30));

            // 메트릭 이름으로 쿼리 예)connectedDeviceCount
            Response<MetricsQueryResult> results = await metricsClient.QueryResourceAsync(iothubResourceId, new[] { "connectedDeviceCount" }, options);

            results.Value.Metrics.ToList().ForEach(x =>
            {
                Console.WriteLine(x);
                x.TimeSeries.ToList().ForEach(y => {

                    y.Values.ToList().ForEach(timeseries =>
                    {
                        //시계열 데이터 출력
                        Console.WriteLine(timeseries.ToString());
                    });
                    
                });
            });

        }

        // 리소스 그룹내 전체 리소스 반환
        private static async Task<ResourceListResult> GetAllResourcesAsync(HttpClient httpClient, string subscriptionId, string resourceGroupName)
        {
            var rest_resources_url = $"https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/resources?api-version=2021-04-01";
            var resp = await client.GetAsync(rest_resources_url);
            var cont = await resp.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ResourceListResult>(cont); // 여기서 오류가 발생되면 Model 을 적절히 업데이트 해야 함.
        }

        // https://learn.microsoft.com/en-us/rest/api/monitor/metrics/list?tabs=HTTP#response
        private static async Task<string> GetMetricsList(HttpClient httpClient, string resourceId)
        {
            //GET
            var rest_resources_url = $" https://management.azure.com/{resourceId}/providers/Microsoft.Insights/metrics?api-version=2018-01-01";
            var resp = await client.GetAsync(rest_resources_url);
            return await resp.Content.ReadAsStringAsync();
        }


        // 이 함수는 가능한 사용하지 말고 Azure.Monitor.Query 패키지를 사용 권장
        private static async Task<string> GetMetricsListWithOptions(HttpClient httpClient, string resourceId,
                                                                                                string timespan = "", string interval = "", string metricnames = "",
                                                                                                string aggregation = "", string top = "", string orderby = "",
                                                                                                string filter = "", string resultType = "", string metricnamespace = "" )
        {
            //GET
            var rest_resources_url = $" https://management.azure.com/{resourceId}/providers/Microsoft.Insights/metrics?api-version=2018-01-01&timespan={timespan}&interval={interval}&metricnames={metricnames}&aggregation={aggregation}&top={top}&orderby={orderby}&$filter={filter}&resultType={resultType}&metricnamespace={metricnamespace}";
            var resp = await client.GetAsync(rest_resources_url);
            return await resp.Content.ReadAsStringAsync();
        }

        // 액세스 토큰 발급
        private static async Task<string> GetAccessToken(string tenantId, string clientId, string clientSecret)
        {
            var credentials = new ClientSecretCredential(tenantId, clientId, clientSecret);
            var result = await credentials.GetTokenAsync(new TokenRequestContext(new[] { "https://management.azure.com/.default" }), CancellationToken.None);
            return result.Token;
        }


    }



}