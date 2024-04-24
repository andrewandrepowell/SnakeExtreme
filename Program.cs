using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System;
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;

namespace SnakeExtreme
{
    public class BrowserServer
    {
        // https://blazor.tips/blazor-how-to-ready-window-dimensions/
        // https://www.thinktecture.com/en/blazor/understanding-and-controlling-the-blazor-webassembly-startup-process/
        // https://www.javatpoint.com/javascript-events
        // https://stackoverflow.com/questions/71598291/what-event-handler-fires-on-resize-of-a-client-side-blazor-page
        private readonly IJSRuntime jsRuntime;
        private IJSObjectReference jsModule;        
        private static Size trueDimensions;
        private class BrowserSize
        {
            public int Width { get; set; }
            public int Height { get; set; }
        }
        public BrowserServer(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
        }        
        public static Size Dimensions 
        { 
            get
            {
                DimensionsUpdated = false;
                return trueDimensions;
            }
            private set
            {
                trueDimensions = value;
                DimensionsUpdated = true;
            }
        }
        public static bool DimensionsUpdated { get; private set; } = false;
        [JSInvokable]
        public void ServiceWindowSizeUpdate(int width, int height)
        {
            Dimensions = new Size(width, height);            
        }
        public async Task ConfigureBrowserServer()
        {
            if (jsModule == null)
                jsModule = await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/utility.js");
            await jsModule.InvokeVoidAsync("registerServiceWindowSizeUpdate", DotNetObjectReference.Create(this));
            var windowSize = await jsModule.InvokeAsync<BrowserSize>("getWindowSize");
            Dimensions = new Size(windowSize.Width, windowSize.Height);
        }
    }
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");
            builder.Services.AddScoped(sp => new HttpClient()
            {
                BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
            });
            builder.Services.AddScoped<BrowserServer>();
            var host = builder.Build();
            var browserService = host.Services.GetService<BrowserServer>();
            await browserService.ConfigureBrowserServer();            
            //var browserSize = await browserService.GetWindowSize();
            //Console.WriteLine($"Window Size: {browserSize.Width} {browserSize.Height}");
            //Console.WriteLine("Testing TEst");
            await host.RunAsync();
        }
    }
}
