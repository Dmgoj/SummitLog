import { createBrowserRouter } from "react-router-dom";
import { App } from "./App";
import { LoginPage } from "./pages/LoginPage";
import { RegisterPage } from "./pages/RegisterPage";
import { ConfirmEmailPage } from "./pages/ConfirmEmailPage";
import { ForgotPasswordPage } from "./pages/ForgotPasswordPage";
import { ResetPasswordPage } from "./pages/ResetPasswordPage";
import { SearchPage } from "./pages/SearchPage";
import { VisitedListPage } from "./pages/VisitedListPage";
import { BucketListPage } from "./pages/BucketListPage";
import { ProfilePage } from "./pages/ProfilePage";
import { PeakDetailPage } from "./pages/PeakDetailPage";
import { ProtectedRoute } from "./components/ProtectedRoute";

export const router = createBrowserRouter([
  {
    path: "/",
    element: <App />,
    children: [
      { index: true, element: <SearchPage /> },
      { path: "login", element: <LoginPage /> },
      { path: "register", element: <RegisterPage /> },
      { path: "confirm-email", element: <ConfirmEmailPage /> },
      { path: "forgot-password", element: <ForgotPasswordPage /> },
      { path: "reset-password", element: <ResetPasswordPage /> },
      { path: "peaks/:id", element: <PeakDetailPage /> },
      {
        element: <ProtectedRoute />,
        children: [
          { path: "visited", element: <VisitedListPage /> },
          { path: "bucket-list", element: <BucketListPage /> },
          { path: "profile", element: <ProfilePage /> },
        ],
      },
    ],
  },
]);
