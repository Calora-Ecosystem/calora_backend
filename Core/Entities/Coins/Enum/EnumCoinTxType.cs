namespace Core.Entities.Coins.Enum;

public enum EnumCoinTxType
{
    /// <summary>Eski Calora → coin almashtirish (olib tashlangan; faqat tarixdagi yozuvlar uchun).</summary>
    CaloraExchange = 1,

    /// <summary>Do'st taklif qilgani uchun bonus (kirim).</summary>
    Referral,

    /// <summary>Marketplace xaridi (chiqim).</summary>
    Purchase,

    /// <summary>Admin tomonidan qo'lda o'zgartirish (kirim/chiqim).</summary>
    Admin,

    /// <summary>Kunlik qadam uchun coin: har <c>StepsPerCoin</c> qadam = 1 coin (kirim).</summary>
    Steps
}
