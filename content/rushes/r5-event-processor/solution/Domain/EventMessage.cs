namespace Domain;

public sealed record EventMessage(string Id, string Type, int Amount);
