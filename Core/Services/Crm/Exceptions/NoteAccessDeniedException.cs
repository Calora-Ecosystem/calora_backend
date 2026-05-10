using Core.Exceptions;

namespace Core.Services.Crm.Exceptions;

public class NoteAccessDeniedException() : ForbiddenException("note_access_denied");
