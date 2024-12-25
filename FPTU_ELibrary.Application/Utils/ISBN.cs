namespace FPTU_ELibrary.Application.Utils;

public class ISBN
{
    public ISBN(string isbn)
    {
        SetIsbn(isbn);
    }
    private string _isbn10 = "";
    private string _isbn13 = "";
    public string ISBN10
    {
        get
        {
            return _isbn10;
        }
        set
        {
            string corrected = "";
            if (!IsValid(value, out corrected) && corrected == "")
                throw new Exception("invalid ISBN");
            SetIsbn(corrected);
        }
    }
    public string ISBN13
    {
        get
        {
            return _isbn13;
        }
        set
        {
            string corrected = "";
            if (!IsValid(value, out corrected) && corrected == "")
                throw new Exception("invalid ISBN");
            SetIsbn(corrected);
        }
    }
    private void SetIsbn(string isbn)
    {
        isbn = CleanIsbn(isbn);
        if (isbn.Length == 10)
        {
            _isbn10 = isbn;
            _isbn13 = Convert10to13(isbn);
        }
        else if (isbn.Length == 13)
        {
            _isbn13 = isbn;
            _isbn10 = Convert13to10(isbn);
        }
    }
    public static string CleanIsbn(string isbn)
    {
        return isbn.Replace("-", "").Replace(" ", "");
    }
    public static string Convert10to13(string isbn)
    {
        return Convert10to13(isbn, true);
    }
    public static string Convert10to13(string isbn, bool throwError) {
        // remove - and space
        string isbn10 = CleanIsbn(isbn);
        if (isbn10.Length != 10 && throwError)
            throw new Exception("ISBN must be 10 characters long");
        // 1) Drop the check digit (the last digit)
        isbn10 = isbn10.Substring(0, 9);
        // 2) Add the prefix '978'
        string isbn13 = "978" + isbn10;
        // 3) Recalculate check digit
        isbn13 = isbn13 + Isbn13Checksum(isbn13);
        return isbn13;
    }
    public static string Convert13to10(string isbn)
    {
        return Convert13to10(isbn, true);
    }
    public static string Convert13to10(string isbn, bool throwError)
    {
        // remove - and space
        string isbn13 = CleanIsbn(isbn);
        if (isbn13.Length != 13 && throwError)
            throw new Exception("ISBN must be 13 characters long");
        // 1) Drop the check digit (the last digit) and prefix '978'
        string isbn10 = isbn13.Substring(3, 9);
        // 2) Recalculate your check digit using the modules 10 check digit routine.
        isbn10 = isbn10 + Isbn10Checksum(isbn10);
        return isbn10;
    }
    
    public static bool IsValid(string isbn)
    {
        string correctIsbn = "";
        return IsValid(isbn, out correctIsbn);
    }
    public static bool IsValid(string isbn, out string correctISBN)
    {
        // remove - and space
        isbn = CleanIsbn(isbn);
        if (isbn.Length == 10) {
            return ValidateIsbn10(isbn, out correctISBN);
        }
        else if (isbn.Length == 13)
        {
            return ValidateIsbn13(isbn, out correctISBN);
        }
        else
        {
            correctISBN = "";
            return false;
        }
    }
    private static string Isbn10Checksum(string isbn)
    {
        int sum = 0;
        for (int i = 0; i < 9; i++)
             sum += (10-i) * Int32.Parse(isbn[i].ToString());
        float div = sum / 11;
        float rem = sum % 11;
        if (rem == 0)
            return "0";
        else if (rem == 1)
            return "X";
        else
            return (11 - rem).ToString();
    }
    private static string Isbn13Checksum(string isbn)
    {
        float sum = 0;
        for (int i = 0; i < 12; i++)
            sum += ((i % 2 == 0) ? 1 : 3) * Int32.Parse(isbn[i].ToString());
        float div = sum / 10;
        float rem = sum % 10;
        if (rem == 0)
            return "0";
        else
            return (10 - rem).ToString();
    }
    private static bool ValidateIsbn10(string isbn, out string correctISBN)
    {
        correctISBN = isbn.Substring(0, 9) + Isbn10Checksum(isbn);
        return (correctISBN == isbn);
    }
    private static bool ValidateIsbn13(string isbn, out string correctISBN)
    {
        correctISBN = isbn.Substring(0, 12) + Isbn13Checksum(isbn);
        return (correctISBN == isbn);
    }
}