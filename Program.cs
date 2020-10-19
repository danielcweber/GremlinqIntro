using System;
using System.Linq;
using System.Threading.Tasks;
using ExRam.Gremlinq.Core;
using ExRam.Gremlinq.Providers.WebSocket;
using Microsoft.Extensions.Logging;
using static ExRam.Gremlinq.Core.GremlinQuerySource;

namespace ExRam.Gremlinq.Samples
{
    public class Program
    {
        private Person _alice;
        private Person _bob;
        private Person _charlie;
        private readonly IGremlinQuerySource _g;

        public Program(IGremlinQuerySource g)
        {
            _g = g;
        }

        public async Task Run()
        {
            await Graph_erstellen();

            await Wen_kennt_Alice();
            await Wer_besitzt_ein_Haustier_dessen_Name_mit_B_anfängt();
            await Wer_besitzt_wieviele_Haustiere();
            await Wer_wohnt_mit_wem_zusammen();
            await Wer_vermietet_ein_Haus();

            Console.Write("Press any key...");
            Console.Read();
        }

        private async Task Graph_erstellen()
        {
            await _g.V().Drop();

            _alice = await _g
                .AddV(new Person { Name = "Alice" })
                .FirstAsync();

            _bob = await _g
                .AddV(new Person { Name = "Bob"  })
                .FirstAsync();

            _charlie = await _g
               .AddV(new Person { Name = "Charlie" })
               .FirstAsync();

            var schillerStrasse17 = await _g
                .AddV(new Haus { Address = "Schillerstr. 17" })
                .FirstAsync();

            var goetheStrasse36 = await _g
                .AddV(new Haus { Address = "Goethestr. 36" })
                .FirstAsync();

            var bello = await _g
                .AddV(new Hund { Name = "Bello" })
                .FirstAsync();

            var lumpi = await _g
                .AddV(new Katze { Name = "Lumpi" })
                .FirstAsync();

            await _g
                .V(_charlie.Id)
                .AddE<Besitzt>()
                .To(__ => __
                    .V(schillerStrasse17.Id));

            await _g
                .V(_charlie.Id)
                .AddE<Besitzt>()
                .To(__ => __
                    .V(goetheStrasse36.Id));

            await _g
                .V(_alice.Id)
                .AddE<Kennt>()
                .To(__ => __
                    .V(_bob.Id));

            await _g
                .V(_alice.Id)
                .AddE<Kennt>()
                .To(__ => __
                    .V(_charlie.Id));

            await _g
                .V(_alice.Id)
                .AddE<Besitzt>()
                .To(__ => __
                    .V(bello.Id));

            await _g
                .V(_bob.Id)
                .AddE<Besitzt>()
                .To(__ => __
                    .V(lumpi.Id));

            await _g
                .V(_alice.Id)
                .AddE<WohntIn>()
                .To(__ => __
                    .V(schillerStrasse17.Id));

            await _g
              .V(_bob.Id)
              .AddE<WohntIn>()
              .To(__ => __
                  .V(schillerStrasse17.Id));

            await _g
              .V(_charlie.Id)
              .AddE<WohntIn>()
              .To(__ => __
                  .V(goetheStrasse36.Id));
        }

        private async Task Wen_kennt_Alice()
        {
            var bekannte = await _g
                .V<Person>()
                .Where(x => x.Name == "Alice")
                .Both<Kennt>()
                .OfType<Person>()
                .Order(x => x
                    .By(x => x.Name));

            Console.WriteLine("Wen kennt Alice?");

            foreach (var person in bekannte)
            {
                Console.WriteLine($" Alice kennt {person.Name}.");
            }

            Console.WriteLine();
        }

        private async Task Wer_besitzt_ein_Haustier_dessen_Name_mit_B_anfängt()
        {
            var besitzer = await _g
                .V<Person>()
                .Where(__ => __
                    .Out<Besitzt>()
                    .OfType<Haustier>()
                    .Where(x => x.Name.StartsWith("B")));

            Console.WriteLine("Wer besitzt ein Haustier, dessen Name mit 'B' anfängt?");

            foreach (var person in besitzer)
            {
                Console.WriteLine($" {person.Name} besitzt ein Haustier, dessen Name mit 'B' anfängt.");
            }

            Console.WriteLine();
        }

        private async Task Wer_besitzt_wieviele_Haustiere()
        {
            var tuples = await _g
                .V<Person>()
                .Project(p => p
                    .ToDynamic()
                    .By("name", __ => __
                        .Values(x => x.Name))
                    .By("anzahl", __ => __
                        .Out<Besitzt>()
                        .OfType<Haustier>()
                        .Count()));

            Console.WriteLine("Wer besitzt wieviele Haustiere ?");

            foreach (var tuple in tuples)
            {
                Console.WriteLine($" {tuple.name} besitzt {tuple.anzahl} Haustier{(tuple.anzahl == 1 ? "" : "e")}.");
            }

            Console.WriteLine();
        }

        private async Task Wer_wohnt_mit_wem_zusammen()
        {
            var mitbewohnerListe = await _g
                .V<Person>()
                .As((__, person) => __
                    .Project(p => p
                        .ToTuple()
                        .By(__ => __
                            .Values(x => x.Name))
                        .By(__ => __
                            .Out<WohntIn>()
                            .In<WohntIn>()
                            .OfType<Person>()
                            .Where(anderePerson => anderePerson != person.Value)
                            .Values(x => x.Name)
                            .Fold())));

            Console.WriteLine("Wer wohnt mit wem zusammen?");

            foreach (var mitbewohner in mitbewohnerListe)
            {
                if (mitbewohner.Item2.Any())
                    Console.WriteLine($" {mitbewohner.Item1} wohnt mit {string.Join(',', mitbewohner.Item2)} zusammen.");
                else
                    Console.WriteLine($" {mitbewohner.Item1} wohnt mit niemandem zusammen.");
            }

            Console.WriteLine();
        }

        private async Task Wer_vermietet_ein_Haus()
        {
            var alleVermieter = await _g
                .V<Person>()
                .As((__, besitzer) => __
                    .Where(__ => __
                        .Out<Besitzt>()
                        .Local(__ => __
                            .In<WohntIn>()
                            .OfType<Person>()
                            .Map(__ => __
                                .Fold())
                            .Where(bewohner => !bewohner.Contains(besitzer.Value)))));

            Console.WriteLine("Wer vermietet ein Haus?");

            foreach (var vermieter in alleVermieter)
            {
                Console.WriteLine($" {vermieter.Name} vermietet ein Haus.");
            }

            Console.WriteLine();
        }

        private static async Task Main()
        {
            var program = new Program(g
                .ConfigureEnvironment(env => env
                    .UseLogger(LoggerFactory
                        .Create(builder => builder
                            .AddFilter(__ => true)
                            .AddConsole())
                        .CreateLogger("Queries"))
                    .UseModel(GraphModel
                        .FromBaseTypes<Knoten, Kante>(lookup => lookup
                            .IncludeAssembliesOfBaseTypes()))
                    .UseGremlinServer(builder => builder
                        .AtLocalhost())
                    .ConfigureOptions(options => options
                        .SetValue(WebSocketGremlinqOptions.QueryLogLogLevel, LogLevel.None))));

            await program.Run();
        }
    }
}
