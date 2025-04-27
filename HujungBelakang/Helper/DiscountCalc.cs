namespace HujungBelakang.Helper
{
    public class DiscountCalc
    {
            public static double GetDiscountRate(long totalCents)
            {
                double baseDiscountRate;

                if (totalCents < 20000)
                {
                    baseDiscountRate = 0.0;
                }
                else if (totalCents >= 20000 && totalCents <= 50000)
                {
                    baseDiscountRate = 0.05;
                }
                else if (totalCents > 50000 && totalCents <= 80000)
                {
                    baseDiscountRate = 0.07;
                }
                else if (totalCents > 80000 && totalCents <= 120000)
                {
                    baseDiscountRate = 0.10;
                }
                else
                {
                    baseDiscountRate = 0.15;
                }

                long totalRinggit = totalCents / 100;

                double extraDiscountRate = 0.0;

                if (totalRinggit > 500 && IsPrime(totalRinggit)) 
                {
                    extraDiscountRate = extraDiscountRate + 0.08;
                }

                if (totalRinggit > 900 && totalRinggit % 10 == 5)
                {
                    extraDiscountRate = extraDiscountRate + 0.10;
                }

                double combinedDiscountRate = baseDiscountRate + extraDiscountRate;
                double finalDiscountRate = Math.Min(combinedDiscountRate, 0.20);

                return finalDiscountRate;
            }

            private static bool IsPrime(long n)
            {

                if (n < 2)
                {
                    return false;
                }

                if (n == 2)
                {
                    return true;
                }

                if (n % 2 == 0)
                {
                    return false;
                }

                long limit = (long)Math.Floor(Math.Sqrt(n));

                for (long i = 3; i <= limit; i = i + 2)
                {
                    if (n % i == 0)
                    {
                        return false;
                    }
                }
                return true;
            }
    }
}
