namespace Core.Interfaces
{
    public interface IOpenAIEmbeddingService
    {
        /// <summary>
        /// Verilen metin için bir gömme (embedding) vektörü oluşturur.
        /// </summary>
        /// <param name="inputText">Gömme oluşturulacak metin.</param>
        /// <returns>Metni temsil eden float değerlerden oluşan bir liste.</returns>
        Task<List<float>> GenerateEmbeddingAsync(string inputText);

        /// <summary>
        /// Kullanıcı girişi ve video içeriğine dayalı bir sohbet yanıtı oluşturur.
        /// </summary>
        /// <param name="userInput">Kullanıcının girdiği metin.</param>
        /// <param name="videoId">Girdiyle ilişkili video kimliği.</param>
        /// <returns>Oluşturulan sohbet yanıtı metni.</returns>
        Task<string> GenerateChatResponseAsync(string userInput, string videoId);

        /// <summary>
        /// İki vektör arasındaki kosinüs benzerliğini hesaplar.
        /// </summary>
        /// <param name="vector1">Birinci vektör.</param>
        /// <param name="vector2">İkinci vektör.</param>
        /// <returns>Kosinüs benzerliği değeri.</returns>
        double CalculateCosineSimilarity(float[] vector1, float[] vector2);
    }

}
