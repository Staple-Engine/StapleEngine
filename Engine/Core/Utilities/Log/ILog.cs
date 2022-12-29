namespace Staple
{
    public interface ILog
    {
        void Info(string message);
        
        void Warning(string message);
        
        void Error(string message);

        void Debug(string message);
    }
}