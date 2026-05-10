using Core.Exceptions;

namespace Core.Services.Crm.Exceptions;

public class NoteNotFoundException() : NotFoundException("note_not_found");
