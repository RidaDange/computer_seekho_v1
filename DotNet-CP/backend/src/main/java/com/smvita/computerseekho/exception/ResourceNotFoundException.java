package com.smvita.computerseekho.exception;

public class ResourceNotFoundException extends RuntimeException {
    public ResourceNotFoundException(String entity, Object id) {
        super(entity + " not found with id: " + id);
    }

    public ResourceNotFoundException(String message) {
        super(message);
    }
}
