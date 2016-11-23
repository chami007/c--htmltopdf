using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using NReco.PhantomJS;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            var json = new JavaScriptSerializer();

            var phantomJS = new PhantomJS();
            phantomJS.OutputReceived += (sender, e) => {
                Console.WriteLine("PhantomJS output: {0}", e.Data);
            };
            phantomJS.ErrorReceived += (sender, e) => {
                Console.WriteLine("PhantomJS error: {0}", e.Data);
            };
            // provide custom input data to js code
            var inputData = json.Serialize(new[] {
                new User() { name = "Bob", age = 30, company = "AirBNB" },
                new User() { name = "Alice", age = 27, company = "Yahoo" },
                new User() { name = "Tom", age = 31, company = "Microsoft" }
            });
            var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(inputData + "\n"));

            try
            {
                phantomJS.RunScript(@"
					var system = require('system');
					console.log('read data...');
					var inputData = system.stdin.readLine();
					console.log('done');
					var input = JSON.parse(inputData);
					for (var i=0; i<input.length; i++) {
						console.log('Name: '+input[i].name+'  Age: '+input[i].age);
					}
					phantom.exit();
				", null, inputStream, null);
            }
            finally
            {
                phantomJS.Abort(); // ensure that phantomjs.exe is stopped
            }

            Console.WriteLine();
            Console.WriteLine();

            // write result to stdout
            Console.WriteLine("Getting content from google.com directly to C# code...");
            var outFileHtml = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "google.html");
            if (File.Exists(outFileHtml))
                File.Delete(outFileHtml);
            using (var outFs = new FileStream(outFileHtml, FileMode.OpenOrCreate, FileAccess.Write))
            {
                try
                {
                    phantomJS.RunScript(@"
						var system = require('system');
						var page = require('webpage').create();
						page.open('https://google.com/', function() {
							system.stdout.writeLine(page.content);
							phantom.exit();
						});
					", null, null, outFs);
                }
                finally
                {
                    phantomJS.Abort(); // ensure that phantomjs.exe is stopped
                }
            }
            Console.WriteLine("Result is saved into " + outFileHtml);

            Console.WriteLine();
            Console.WriteLine();

            // execute rasterize.js
            var outFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "google.pdf");
            if (File.Exists(outFile))
                File.Delete(outFile);
            Console.WriteLine("Getting screenshot of google.com page...");
            try
            {
                phantomJS.Run(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rasterize.js"),
                    new[] { "http://nvd3.org/examples/discreteBar.html", outFile });
            }
            finally
            {
                phantomJS.Abort();
            }
            Console.WriteLine("Result is saved into " + outFile);
        }
    }

    public class User
    {
        public string name { get; set; }
        public int age { get; set; }
        public string company { get; set; }
    }
}
