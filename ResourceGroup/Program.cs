using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace ResourceGroup
{
    internal class Program
    {
        static async Task Main(string[] args)
        {

            // appsettings.local.json 파일을 생성하고, subscriptionid 를 넣어야 함, 파일 속성에서 Copy to Output Directory 를 Copy Always 로 설정해야 함
            // az account show --query id -o tsv 로 subscriptionid 확인 가능
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var subscriptionId = config["subscriptionid"];


            // subscriptionId 를 명시적으로 선언하기 위해서는 다음코드 사용 필요.
            //var armClient = new ArmClient(new DefaultAzureCredential(), subscriptionId);
            //var subscription = armClient.GetDefaultSubscription();

            // Azure.Identity.DefaultAzureCredential 를 사용하면, 로컬에서는 Visual Studio에서 로그인한 계정을 사용하고, Azure 에서는 Managed Identity를 사용함
            var armClient = new ArmClient(new DefaultAzureCredential());

            var subscription = await armClient.GetDefaultSubscriptionAsync();

            var resourcegroups = subscription.GetResourceGroups();

            // 리소스 그룹 목록 출력
            resourcegroups.ToList().ForEach(rg =>
            {
                Console.WriteLine(rg.Data.Name);
            });

            Console.ReadKey();           

        }
    }
}