using Core.Exceptions;

namespace Core.Services.Crm.Exceptions;

/// <summary>
/// Thrown when a lead is moved along a path the pipeline forbids — chiefly a "Yangi" (New)
/// lead that must first be moved to "Bog'lanish" (Contacted) before any other stage.
/// </summary>
public class InvalidLeadTransitionException()
    : BadRequestException("Yangi leadni avval \"Bog'lanish\"ga o'tkazish kerak");
