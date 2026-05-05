from fastapi import HTTPException, status


class ServiceUnavailableError(HTTPException):
    def __init__(self, detail: str = "Analysis service unavailable"):
        super().__init__(status_code=status.HTTP_503_SERVICE_UNAVAILABLE, detail=detail)


class NoDataError(HTTPException):
    def __init__(self, detail: str = "No benchmark data found"):
        super().__init__(status_code=status.HTTP_404_NOT_FOUND, detail=detail)


class DatabaseError(HTTPException):
    def __init__(self, detail: str = "Database error"):
        super().__init__(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail=detail)
