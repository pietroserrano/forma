using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using Forma.Abstractions;
using Forma.Core.Abstractions;
using Forma.Mediator;
using Forma.Mediator.Extensions;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Forma.Benchmarks
{
    /// <summary>
    /// Benchmark di confronto tra Forma.Mediator e MediatR di Jimmy Bogard
    /// </summary>    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    public class MediatRComparisonBenchmarks
    {
        // Mediator instances
        private IRequestMediator _formaMediator = null!;
        private MediatR.IMediator _mediatRMediator = null!;

        // Test request objects
        private FormaSimpleRequest _formaRequest = null!;
        private FormaRequestWithResponseObj _formaResponseRequest = null!;
        private MediatRSimpleRequest _mediatRRequest = null!;
        private MediatRRequestWithResponseObj _mediatRResponseRequest = null!;

        [GlobalSetup]
        public void Setup()
        {
            // ===== Setup per Forma.Mediator =====
            var formaServices = new ServiceCollection();

            // Registrazione degli handler
            formaServices.AddTransient<IHandler<FormaSimpleRequest>, FormaSimpleRequestHandler>();
            formaServices.AddTransient<IHandler<FormaRequestWithResponseObj, string>, FormaRequestWithResponseHandler>();

            // Configurazione del mediator
            formaServices.AddRequestMediator(config => { });

            var formaServiceProvider = formaServices.BuildServiceProvider();
            _formaMediator = formaServiceProvider.GetRequiredService<IRequestMediator>();

            _formaRequest = new FormaSimpleRequest { Data = "Test data" };
            _formaResponseRequest = new FormaRequestWithResponseObj { Data = "Test data" };

            // ===== Setup per MediatR =====
            var mediatRServices = new ServiceCollection();

            // Registrazione MediatR
            mediatRServices.AddMediatR(typeof(MediatRComparisonBenchmarks));

            var mediatRServiceProvider = mediatRServices.BuildServiceProvider();
            _mediatRMediator = mediatRServiceProvider.GetRequiredService<MediatR.IMediator>();

            _mediatRRequest = new MediatRSimpleRequest { Data = "Test data" };
            _mediatRResponseRequest = new MediatRRequestWithResponseObj { Data = "Test data" };
        }
        [Benchmark]
        [BenchmarkCategory("SendAsObject")]
        public async Task Forma_SendAsync_object()
        {
            await _formaMediator.SendAsync((object)_formaRequest);
        }

        [Benchmark]
        [BenchmarkCategory("SimpleRequest")]
        public async Task Forma_SimpleRequest()
        {
            await _formaMediator.SendAsync(_formaRequest);
        }

        [Benchmark]
        [BenchmarkCategory("RequestWithResponse")]
        public async Task<string> Forma_RequestWithResponse()
        {
            return await _formaMediator.SendAsync(_formaResponseRequest);
        }

        [Benchmark]
        [BenchmarkCategory("SendAsObject")]
        public async Task MediatR_Send_object()
        {
            await _mediatRMediator.Send((object)_mediatRRequest);
        }
        [Benchmark]
        [BenchmarkCategory("SimpleRequest")]
        public async Task MediatR_SimpleRequest()
        {
            await _mediatRMediator.Send(_mediatRRequest);
        }

        [Benchmark]
        [BenchmarkCategory("RequestWithResponse")]
        public async Task<string> MediatR_RequestWithResponse()
        {
            return await _mediatRMediator.Send(_mediatRResponseRequest);
        }
    }

    #region Forma.Mediator classes
    public class FormaSimpleRequest : Forma.Abstractions.IRequest
    {
        public string Data { get; set; } = string.Empty;
    }

    public class FormaRequestWithResponseObj : Forma.Abstractions.IRequest<string>
    {
        public string Data { get; set; } = string.Empty;
    }

    public class FormaSimpleRequestHandler : IHandler<FormaSimpleRequest>
    {
        public Task HandleAsync(FormaSimpleRequest request, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    public class FormaRequestWithResponseHandler : IHandler<FormaRequestWithResponseObj, string>
    {
        public Task<string> HandleAsync(FormaRequestWithResponseObj request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult($"Response: {request.Data}");
        }
    }
    #endregion

    #region MediatR classes
    public class MediatRSimpleRequest : MediatR.IRequest
    {
        public string Data { get; set; } = string.Empty;
    }

    public class MediatRRequestWithResponseObj : MediatR.IRequest<string>
    {
        public string Data { get; set; } = string.Empty;
    }

    public class MediatRSimpleRequestHandler : MediatR.IRequestHandler<MediatRSimpleRequest>
    {
        public Task<MediatR.Unit> Handle(MediatRSimpleRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(MediatR.Unit.Value);
        }
    }

    public class MediatRRequestWithResponseHandler : MediatR.IRequestHandler<MediatRRequestWithResponseObj, string>
    {
        public Task<string> Handle(MediatRRequestWithResponseObj request, CancellationToken cancellationToken)
        {
            return Task.FromResult($"Response: {request.Data}");
        }
    }
    #endregion
}
