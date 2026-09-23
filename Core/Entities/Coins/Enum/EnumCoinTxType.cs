namespace Core.Entities.Coins.Enum;

public enum EnumCoinTxType
{
    /// <summary>Calora → coin almashtirish (kirim).</summary>
    CaloraExchange = 1,

    /// <summary>Do'st taklif qilgani uchun bonus (kirim).</summary>
    Referral,

    /// <summary>Marketplace xaridi (chiqim).</summary>
    Purchase,

    /// <summary>Admin tomonidan qo'lda o'zgartirish (kirim/chiqim).</summary>
    Admin
}
