namespace DoubleYou.Domain.DTOs
{
    public sealed class DTO
    {
        public sealed record SaveUser(
                string TranslationLanguage,
                string FavoriteTopic
            );
    }
}