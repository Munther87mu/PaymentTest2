using Microsoft.AspNetCore.Mvc;
using System.IO.Ports;
using System.Text;
using TbpEcr;

namespace PaymentTest.Controllers
{
    [ApiController]
    [Route("pos")]
    public class PaymentController : ControllerBase
    {
        private TbpEcrConnector ecrConnector;
        private readonly object lockObject = new object();

        public PaymentController()
        {
            try
            {
                ecrConnector = new TbpEcrConnector(new EcrTransportCOMWrapper("COM6"));
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to initialize the POS controller. Error: " + ex.Message);
            }
        }


        [HttpPost("sale")]
        public IActionResult PerformSale([FromBody] SaleRequest request)
        {
            lock (lockObject)
            {
                try
                {
                    decimal amount = request.Amount;
                    string[] receiptText = request.ReceiptText;
                    string EcrInvoice = request.ecrInvoice;

                    SaleRequestData saleRequestData = new SaleRequestData
                    {
                        Amount = (long)amount,
                        ReceiptText = receiptText,
                        ecrInvoice = EcrInvoice
                    };

                    // Perform the sale operation using the TbpEcrConnector
                    TbpEcr.EcrResponse response = ecrConnector.SendSaleRequest(saleRequestData.ecrInvoice, saleRequestData.Amount, saleRequestData.ReceiptText);
                    EcrResponse customResponse = new EcrResponse(response.isSuccess, response.errorCode, response.responseText);


                    if (response.isSuccess)
                    {
                        // Sale operation was successful
                        return Ok("Sale operation completed successfully.");
                    }
                    else
                    {
                        // Sale operation failed
                        return BadRequest($"Failed to perform sale operation: {response.responseText}");
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest($"An error occurred while performing the sale operation: {ex.Message}");
                }
            }
        }
    }

    public class SaleRequest
    {
        public decimal Amount { get; set; }
        public string[] ReceiptText { get; set; }
        public string ecrInvoice { get; set; }
    }

    public class EcrResponse
    {
        public bool IsSuccess { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }

        public EcrResponse(bool isSuccess, string errorCode, string errorMessage)
        {
            IsSuccess = isSuccess;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }
    }

    public class SaleRequestData
    {
        public long Amount { get; set; }
        public string[] ReceiptText { get; set; }
        public string ecrInvoice { get; set; }
    }

    public class EcrTransportCOM : IEcrTransport
    {
        private SerialPort serial;

        private ILowLevelProtocol lowLevelProtocol = new LowLevelProtocolStxEtx();

        private int readTimeoutMs = 1000;

        public EcrTransportCOM(string portName)
            : this(portName, 115200, Parity.None, 8, StopBits.One)
        {
        }

        public EcrTransportCOM(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            serial = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
        }

        ~EcrTransportCOM()
        {
            Close();
        }

        public void Open()
        {
            if (!serial.IsOpen)
            {
                serial.Open();
            }
        }

        public void Close()
        {
            if (serial.IsOpen)
            {
                serial.Close();
            }
        }

        public bool SetTimeouts(int connectionTimeoutMs, int sendTimeoutMs, int recvTimeoutMs)
        {
            if (sendTimeoutMs < 1 || recvTimeoutMs < 1)
            {
                return false;
            }

            readTimeoutMs = recvTimeoutMs;
            serial.WriteTimeout = sendTimeoutMs;
            return true;
        }

        public Result Send(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            byte[] array = lowLevelProtocol.encode(bytes);
            if (array == null)
            {
                return new Result(ResultCode.ERR_ENCODE);
            }

            try
            {
                serial.Write(array, 0, array.Length);
            }
            catch (TimeoutException)
            {
                return new Result(ResultCode.ERR_TIMEOUT);
            }

            return new Result(ResultCode.OK);
        }

        public Result Receive()
        {
            long num = DateTime.UtcNow.Ticks / 10000 + readTimeoutMs;
            long num2 = readTimeoutMs;
            byte[] array = new byte[512];
            do
            {
                int len;
                try
                {
                    serial.ReadTimeout = (int)num2;
                    len = serial.Read(array, 0, array.Length);
                }
                catch (TimeoutException)
                {
                    return new Result(ResultCode.ERR_TIMEOUT);
                }

                LowLevelResult lowLevelResult = lowLevelProtocol.decode(array, len);
                if (lowLevelResult.status == LowLevelStatus.DONE)
                {
                    string @string = Encoding.UTF8.GetString(lowLevelResult.data, 0, lowLevelResult.data.Length);
                    return new Result(@string);
                }

                num2 = num - DateTime.UtcNow.Ticks / 10000;
            }
            while (num2 > 0);
            return new Result(ResultCode.ERR_TIMEOUT);
        }
    }

    public interface IEcrTransport
    {
        void Open();
        void Close();
        bool SetTimeouts(int connectionTimeoutMs, int sendTimeoutMs, int recvTimeoutMs);
        Result Send(string text);
        Result Receive();
    }

    public class Result
    {
        public ResultCode code { get; private set; }
        public string text { get; private set; }

        public Result(ResultCode code)
        {
            this.code = code;
            this.text = "";
        }

        public Result(string text)
        {
            this.code = ResultCode.OK;
            this.text = text;
        }
    }

    public enum ResultCode
    {
        OK,
        ERR_TIMEOUT,
        ERR_ENCODE,
        ERR_DECODE
    }

    public class LowLevelProtocolStxEtx : ILowLevelProtocol
    {
        public byte[] encode(byte[] data)
        {
            // Encoding logic here
            return null;
        }

        public LowLevelResult decode(byte[] data, int length)
        {
            // Decoding logic here
            return null;
        }
    }

    public interface ILowLevelProtocol
    {
        byte[] encode(byte[] data);
        LowLevelResult decode(byte[] data, int length);
    }

    public class LowLevelResult
    {
        public LowLevelStatus status { get; set; }
        public byte[] data { get; set; }
    }

    public enum LowLevelStatus
    {
        DONE,
        INCOMPLETE
    }
}
