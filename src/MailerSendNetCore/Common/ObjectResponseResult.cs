namespace MailerSendNetCore.Common
{
    internal struct ObjectResponseResult<T>
    {
        public ObjectResponseResult(T responseObject, string responseText)
        {
            this.Object = responseObject;
            this.Text = responseText;
        }
        public T Object { get; }
        public string Text { get; }
    }
}
