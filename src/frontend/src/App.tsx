import { BrowserRouter, Routes, Route } from "react-router-dom";
import AppShell from "./layout/AppShell";
import HomePage from "./pages/HomePage";
import LoginPage from "./pages/LoginPage";
import ContactPage from "./pages/ContactPage";
import ScenarioConfigPage from "./pages/ScenarioConfigPage";
import AirportsPage from "./pages/AirportsPage";

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<AppShell />}>
          <Route path="/" element={<HomePage />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/contact" element={<ContactPage />} />
          <Route path="/scenario-config" element={<ScenarioConfigPage />} />
          <Route path="/airports" element={<AirportsPage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}