using Altinn.App.Core.Features.Payment.Exceptions;
using Altinn.App.Core.Features.Payment.Services;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Pdf;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process.ProcessTasks
{
    /// <summary>
    /// Represents the process task responsible for collecting user payment.
    /// </summary>
    internal sealed class PaymentProcessTask : IProcessTask
    {
        private readonly IPdfService _pdfService;
        private readonly IDataClient _dataClient;
        private readonly IProcessReader _processReader;
        private readonly IPaymentService _paymentService;

        private const string PdfContentType = "application/pdf";
        private const string ReceiptFileName = "Betalingskvittering.pdf";

        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentProcessTask"/> class.
        /// </summary>
        public PaymentProcessTask(
            IPdfService pdfService,
            IDataClient dataClient,
            IProcessReader processReader,
            IPaymentService paymentService
        )
        {
            _pdfService = pdfService;
            _dataClient = dataClient;
            _processReader = processReader;
            _paymentService = paymentService;
        }

        /// <inheritdoc/>
        public string Type => "payment";

        /// <inheritdoc/>
        public async Task Start(string taskId, Instance instance)
        {
            AltinnPaymentConfiguration paymentConfiguration = GetAltinnPaymentConfiguration(taskId);
            await _paymentService.CancelAndDeleteAnyExistingPayment(instance, paymentConfiguration);
        }

        /// <inheritdoc/>
        public async Task End(string taskId, Instance instance)
        {
            AltinnPaymentConfiguration paymentConfiguration = GetAltinnPaymentConfiguration(taskId);

            if (!await _paymentService.IsPaymentCompleted(instance, paymentConfiguration))
                throw new PaymentException("The payment is not completed.");

            Stream pdfStream = await _pdfService.GeneratePdf(instance, taskId, CancellationToken.None);

            // ! TODO: restructure code to avoid assertion. Codepaths above have already validated this field
            var paymentDataType = paymentConfiguration.PaymentDataType!;

            await _dataClient.InsertBinaryData(
                instance.Id,
                paymentDataType,
                PdfContentType,
                ReceiptFileName,
                pdfStream,
                taskId
            );
        }

        /// <inheritdoc/>
        public async Task Abandon(string taskId, Instance instance)
        {
            AltinnPaymentConfiguration paymentConfiguration = GetAltinnPaymentConfiguration(taskId);
            await _paymentService.CancelAndDeleteAnyExistingPayment(instance, paymentConfiguration);
        }

        private AltinnPaymentConfiguration GetAltinnPaymentConfiguration(string taskId)
        {
            AltinnPaymentConfiguration? paymentConfiguration = _processReader
                .GetAltinnTaskExtension(taskId)
                ?.PaymentConfiguration;

            if (paymentConfiguration == null)
            {
                throw new ApplicationConfigException(
                    "PaymentConfig is missing in the payment process task configuration."
                );
            }

            if (string.IsNullOrWhiteSpace(paymentConfiguration.PaymentDataType))
            {
                throw new ApplicationConfigException(
                    "PaymentDataType is missing in the payment process task configuration."
                );
            }

            return paymentConfiguration;
        }
    }
}
