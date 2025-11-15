namespace AITS.Api.Validation;

public static class PolishTaxIdValidator
{
    /// <summary>
    /// Waliduje numer NIP (Numer Identyfikacji Podatkowej)
    /// </summary>
    /// <param name="nip">Numer NIP do walidacji</param>
    /// <returns>True jeśli NIP jest poprawny, false w przeciwnym razie</returns>
    public static bool IsValidNip(string? nip)
    {
        if (string.IsNullOrWhiteSpace(nip))
            return false;

        // Usuń spacje i myślniki
        var cleanNip = nip.Replace(" ", "").Replace("-", "");

        // NIP powinien mieć 10 cyfr
        if (cleanNip.Length != 10 || !cleanNip.All(char.IsDigit))
            return false;

        // Sprawdź sumę kontrolną
        int[] weights = { 6, 5, 7, 2, 3, 4, 5, 6, 7 };
        int sum = 0;

        for (int i = 0; i < 9; i++)
        {
            sum += int.Parse(cleanNip[i].ToString()) * weights[i];
        }

        int checkDigit = sum % 11;
        if (checkDigit == 10)
            return false;

        return checkDigit == int.Parse(cleanNip[9].ToString());
    }

    /// <summary>
    /// Waliduje numer REGON (Rejestr Gospodarki Narodowej)
    /// </summary>
    /// <param name="regon">Numer REGON do walidacji</param>
    /// <returns>True jeśli REGON jest poprawny, false w przeciwnym razie</returns>
    public static bool IsValidRegon(string? regon)
    {
        if (string.IsNullOrWhiteSpace(regon))
            return false;

        // Usuń spacje i myślniki
        var cleanRegon = regon.Replace(" ", "").Replace("-", "");

        // REGON może mieć 9 lub 14 cyfr
        if (cleanRegon.Length != 9 && cleanRegon.Length != 14)
            return false;

        if (!cleanRegon.All(char.IsDigit))
            return false;

        // Walidacja dla 9-cyfrowego REGON
        if (cleanRegon.Length == 9)
        {
            int[] weights = { 8, 9, 2, 3, 4, 5, 6, 7 };
            int sum = 0;

            for (int i = 0; i < 8; i++)
            {
                sum += int.Parse(cleanRegon[i].ToString()) * weights[i];
            }

            int checkDigit = sum % 11;
            if (checkDigit == 10)
                checkDigit = 0;

            return checkDigit == int.Parse(cleanRegon[8].ToString());
        }

        // Walidacja dla 14-cyfrowego REGON
        if (cleanRegon.Length == 14)
        {
            // Sprawdź pierwsze 9 cyfr (REGON główny)
            if (!IsValidRegon(cleanRegon.Substring(0, 9)))
                return false;

            // Sprawdź sumę kontrolną dla pozostałych 5 cyfr
            int[] weights = { 2, 4, 8, 5, 0, 9, 7, 3, 6, 1, 2, 4, 8 };
            int sum = 0;

            for (int i = 0; i < 13; i++)
            {
                sum += int.Parse(cleanRegon[i].ToString()) * weights[i];
            }

            int checkDigit = sum % 11;
            if (checkDigit == 10)
                checkDigit = 0;

            return checkDigit == int.Parse(cleanRegon[13].ToString());
        }

        return false;
    }

    /// <summary>
    /// Formatuje NIP do standardowego formatu (XXX-XXX-XX-XX)
    /// </summary>
    public static string? FormatNip(string? nip)
    {
        if (string.IsNullOrWhiteSpace(nip))
            return null;

        var cleanNip = nip.Replace(" ", "").Replace("-", "");
        if (cleanNip.Length != 10)
            return nip;

        return $"{cleanNip.Substring(0, 3)}-{cleanNip.Substring(3, 3)}-{cleanNip.Substring(6, 2)}-{cleanNip.Substring(8, 2)}";
    }

    /// <summary>
    /// Formatuje REGON do standardowego formatu
    /// </summary>
    public static string? FormatRegon(string? regon)
    {
        if (string.IsNullOrWhiteSpace(regon))
            return null;

        var cleanRegon = regon.Replace(" ", "").Replace("-", "");
        if (cleanRegon.Length == 9)
        {
            return $"{cleanRegon.Substring(0, 3)}-{cleanRegon.Substring(3, 3)}-{cleanRegon.Substring(6, 3)}";
        }
        else if (cleanRegon.Length == 14)
        {
            return $"{cleanRegon.Substring(0, 3)}-{cleanRegon.Substring(3, 3)}-{cleanRegon.Substring(6, 3)}-{cleanRegon.Substring(9, 5)}";
        }

        return regon;
    }
}


