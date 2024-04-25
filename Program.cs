using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;

namespace SnakeExtreme
{
    public class BrowserService
    {
        // https://blazor.tips/blazor-how-to-ready-window-dimensions/
        // https://www.thinktecture.com/en/blazor/understanding-and-controlling-the-blazor-webassembly-startup-process/
        // https://www.javatpoint.com/javascript-events
        // https://stackoverflow.com/questions/71598291/what-event-handler-fires-on-resize-of-a-client-side-blazor-page
        // https://www.w3schools.com/jsref/obj_touchevent.asp
        // https://www.w3schools.com/jsref/dom_obj_event.asp
        // https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/call-dotnet-from-javascript?view=aspnetcore-8.0
        private readonly IJSRuntime jsRuntime;
        private IJSObjectReference jsModule;        
        private static Size trueDimensions;
        private class BrowserSize
        {
            public int Width { get; set; }
            public int Height { get; set; }
        }
        public class Touch
        {
            public float X { get; set; }
            public float Y { get; set; }
        }
        public BrowserService(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
        }
        public static Queue<Vector2> Touches { get; } = new();
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
        public string GetLog()
        {
            return $"Touch Connected: {TouchPanel.GetCapabilities().IsConnected}";
        }
        [JSInvokable]
        public void ServiceWindowSizeUpdate(int width, int height)
        {
            Dimensions = new Size(width, height);            
        }
        [JSInvokable]
        public void ServiceTouchStartUpdate(Touch[] touches)
        {            
            foreach (var touch in touches)
                if (Touches.Count < 64)
                    Touches.Enqueue(new Vector2(touch.X, touch.Y));
        }
        public async Task ConfigureBrowserServer()
        {
            if (jsModule == null)
                jsModule = await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/utility.js");
            await jsModule.InvokeVoidAsync("registerServiceUpdates", DotNetObjectReference.Create(this));
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
            builder.Services.AddScoped<BrowserService>();
            var host = builder.Build();
            var browserService = host.Services.GetService<BrowserService>();
            await browserService.ConfigureBrowserServer();            
            await host.RunAsync();
        }
    }
}
