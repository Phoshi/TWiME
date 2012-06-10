static internal class RomanNumeral {
    private static int[] values = new[] { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
    private static string[] numerals = new[] { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };

    public static string ToRoman(int number) {
        if (number == 0) {
            return "N";
        }

        string converted = "";

        for (int i = 0; i < 13; i++) {
            while (number >= values[i]) {
                number -= values[i];
                converted+=numerals[i];
            }
        }
        return converted;
    }
}