using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Polymarket.Net.Interfaces.Clients.ClobApi;
using Polymarket.Net.Objects;
using Polymarket.Net.Objects.Options;
using Polymarket.Net.Utils;

namespace Polymarket.Net.UnitTests
{
    [TestFixture]
    public class CancellationFlowTests
    {
        [Test]
        public void GetTokenInfoAsync_ThrowsWhenCanceledBeforeEnter()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var client = new Mock<IPolymarketRestClientClobApi>(MockBehavior.Strict);

            Assert.ThrowsAsync<TaskCanceledException>(async () =>
                await PolymarketUtils.GetTokenInfoAsync("token-1", client.Object, cts.Token));
        }

        [Test]
        public async Task GetTokenInfoAsync_ForwardsCancellationTokenToExchangeData()
        {
            var exchangeData = new Mock<IPolymarketRestClientClobApiExchangeData>(MockBehavior.Strict);
            using var cts = new CancellationTokenSource();
            exchangeData
                .Setup(x => x.GetOrderBookAsync("token-1", It.Is<CancellationToken>(ct => ct == cts.Token)))
                .ThrowsAsync(new OperationCanceledException(cts.Token));

            var client = new Mock<IPolymarketRestClientClobApi>(MockBehavior.Strict);
            client.SetupGet(x => x.ClientOptions).Returns(new PolymarketRestOptions
            {
                Environment = PolymarketEnvironment.CreateCustom(
                    "CancellationFlowTests",
                    137,
                    "https://clob.polymarket.com",
                    "https://gamma.polymarket.com",
                    "wss://clob.polymarket.com/ws",
                    "wss://gamma.polymarket.com/ws")
            });
            client.SetupGet(x => x.ExchangeData).Returns(exchangeData.Object);

            Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await PolymarketUtils.GetTokenInfoAsync("token-1", client.Object, cts.Token));
            exchangeData.VerifyAll();
        }
    }
}
