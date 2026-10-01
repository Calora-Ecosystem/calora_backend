namespace Core.Services.Coins.Exceptions;

public class InsufficientCoinsException() : Core.Exceptions.BadRequestException("insufficient_coins");

public class MarketItemNotFoundException() : Core.Exceptions.NotFoundException("market_item_not_found");

public class MarketItemInUseException() : Core.Exceptions.BadRequestException("market_item_in_use");

public class ReferralCodeNotFoundException() : Core.Exceptions.NotFoundException("referral_code_not_found");

public class ReferralAlreadyAppliedException() : Core.Exceptions.BadRequestException("referral_already_applied");

public class ReferralSelfException() : Core.Exceptions.BadRequestException("referral_self");

public class ReferralWindowExpiredException() : Core.Exceptions.BadRequestException("referral_window_expired");

public class MarketItemInvalidException() : Core.Exceptions.BadRequestException("market_item_invalid");

public class CoinRuleNotFoundException() : Core.Exceptions.NotFoundException("coin_rule_not_found");

/// <summary>Qoida faqat bugundan yoki kelajakdan kuchga kirishi mumkin.</summary>
public class CoinRulePastDateException() : Core.Exceptions.BadRequestException("coin_rule_past_date");

/// <summary>O'tgan kunlardagi qoida tarixini o'zgartirib bo'lmaydi.</summary>
public class CoinRuleLockedException() : Core.Exceptions.BadRequestException("coin_rule_locked");

public class CoinRuleInvalidException() : Core.Exceptions.BadRequestException("coin_rule_invalid");

public class CoinEarnStartInvalidException() : Core.Exceptions.BadRequestException("coin_earn_start_invalid");
