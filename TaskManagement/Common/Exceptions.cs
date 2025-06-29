namespace TaskManagement.Common;

public class ConflictException(string message) : Exception(message);

public class NotFoundException(string message) : Exception(message);
