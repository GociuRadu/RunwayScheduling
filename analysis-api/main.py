import json
import os
import uuid
from datetime import datetime

from dotenv import load_dotenv
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel

load_dotenv()

from database import fetch_scenario_data
from claude_service import get_optimal_params

app = FastAPI(title="GA Parameter Analysis API")

RESULTS_DIR = "results"


class AnalyzeRequest(BaseModel):
    scenario_config_id: uuid.UUID
    description: str


@app.post("/analyze")
async def analyze(req: AnalyzeRequest):
    data = await fetch_scenario_data(req.scenario_config_id)
    if not data:
        raise HTTPException(status_code=404, detail="ScenarioConfig not found")

    result = get_optimal_params(req.description, data)

    os.makedirs(RESULTS_DIR, exist_ok=True)
    timestamp = datetime.utcnow().strftime("%Y%m%d_%H%M%S")
    filename = f"{RESULTS_DIR}/{req.scenario_config_id}_{timestamp}.json"
    with open(filename, "w", encoding="utf-8") as f:
        json.dump(
            {
                "scenario_config_id": str(req.scenario_config_id),
                "description": req.description,
                "result": result,
            },
            f,
            indent=2,
        )

    return {"result": result, "saved_to": filename}


@app.get("/results")
def list_results():
    os.makedirs(RESULTS_DIR, exist_ok=True)
    files = sorted(os.listdir(RESULTS_DIR), reverse=True)
    return {"files": files}
