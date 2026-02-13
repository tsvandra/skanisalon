namespace Soluvion.API.Models.Enums
{
    public enum TranslationStatus
    {
        Created = 0,        // Hozzáadva, de még üres/folyamatban
        Translating = 1,    // AI fordítás folyamatban (háttérben)
        ReviewPending = 2,  // AI végzett, Admin jóváhagyásra vár (Banner aktív)
        Published = 3,      // Publikus, látható a vendégeknek
        Error = 99          // Hiba történt a fordítás során
    }
}