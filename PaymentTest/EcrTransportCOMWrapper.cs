using TbpEcr.Transport;

namespace PaymentTest
{
    public class EcrTransportCOMWrapper : IEcrTransport
    {
        private EcrTransportCOM ecrTransport;

        public EcrTransportCOMWrapper(string portName)
        {
            ecrTransport = new EcrTransportCOM(portName);
        }

        public void Open()
        {
            ecrTransport.Open();
        }

        public void Close()
        {
            ecrTransport.Close();
        }

        public bool SetTimeouts(int connectionTimeoutMs, int sendTimeoutMs, int recvTimeoutMs)
        {
            return ecrTransport.SetTimeouts(connectionTimeoutMs, sendTimeoutMs, recvTimeoutMs);
        }

        public Result Send(string text)
        {
            return ecrTransport.Send(text);
        }

        public Result Receive()
        {
            return ecrTransport.Receive();
        }
    }
}
