namespace Core.Services.Coins.Exceptions;

public class InsufficientCoinsException() : Core.Exceptions.BadRequestException("insufficient_coins");

public class NothingToExchangeException() : Core.Exceptions.BadRequestException("nothing_to_exchange");

public class MarketItemNotFoundException() : Core.Exceptions.NotFoundException("market_item_not_found");

public class MarketItemInUseException() : Core.Exceptions.BadRequestException("market_item_in_use");

public class ReferralCodeNotFoundException() : Core.Exceptions.NotFoundException("referral_code_not_found");

public class ReferralAlreadyAppliedException() : Core.Exceptions.BadRequestException("referral_already_applied");

public class ReferralSelfException() : Core.Exceptions.BadRequestException("referral_self");

public class ReferralWindowExpiredException() : Core.Exceptions.BadRequestException("referral_window_expired");
