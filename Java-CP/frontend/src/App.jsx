import { Suspense, lazy } from "react";
import { Routes, Route } from "react-router-dom";
import ProtectedRoute from "./routes/ProtectedRoute";
import AdminLayout from "./components/layout/AdminLayout";
import PublicLayout from "./components/layout/PublicLayout";
import { MASTER_DATA_ROLES, CRM_ROLES } from "./store/slices/authSlice";

// Frontend requirement: Lazy Loading — every route-level page is code-split
// via React.lazy so nothing outside the current route ships to the browser.
const Home = lazy(() => import("./pages/public/Home"));
const CourseDetail = lazy(() => import("./pages/public/CourseDetail"));
const CampusLife = lazy(() => import("./pages/public/CampusLife"));
const BatchAlbum = lazy(() => import("./pages/public/BatchAlbum"));
const GalleryCategory = lazy(() => import("./pages/public/GalleryCategory"));
const Faculty = lazy(() => import("./pages/public/Faculty"));
const GetInTouch = lazy(() => import("./pages/public/GetInTouch"));
const EnquiryForm = lazy(() => import("./pages/public/EnquiryForm"));
const OAuthCallback = lazy(() => import("./pages/public/OAuthCallback"));
const OurRecruiters = lazy(() => import("./pages/public/OurRecruiters"));
const RecruiterDetail = lazy(() => import("./pages/public/RecruiterDetail"));
const BatchwisePlacement = lazy(() => import("./pages/public/BatchwisePlacement"));
const PlacementDetail = lazy(() => import("./pages/public/PlacementDetail"));

const Login = lazy(() => import("./pages/admin/Login"));
const FollowupList = lazy(() => import("./pages/admin/FollowupList"));
const Inquiries = lazy(() => import("./pages/admin/Inquiries"));
const AddEnquiry = lazy(() => import("./pages/admin/AddEnquiry"));
const Registration = lazy(() => import("./pages/admin/Registration"));
const Students = lazy(() => import("./pages/admin/Students"));
const BatchManagement = lazy(() => import("./pages/admin/BatchManagement"));
const PlacementEntry = lazy(() => import("./pages/admin/PlacementEntry"));
const RecruitersMaintenance = lazy(() => import("./pages/admin/RecruitersMaintenance"));
const AnnouncementsMaintenance = lazy(() => import("./pages/admin/AnnouncementsMaintenance"));
const ClosureReasonsMaintenance = lazy(() => import("./pages/admin/ClosureReasonsMaintenance"));
const CourseCategoriesMaintenance = lazy(() => import("./pages/admin/CourseCategoriesMaintenance"));
const CoursesMaintenance = lazy(() => import("./pages/admin/CoursesMaintenance"));
const BannersMaintenance = lazy(() => import("./pages/admin/BannersMaintenance"));
const TestimonialsMaintenance = lazy(() => import("./pages/admin/TestimonialsMaintenance"));
const NewsEventsMaintenance = lazy(() => import("./pages/admin/NewsEventsMaintenance"));
const GalleryImagesMaintenance = lazy(() => import("./pages/admin/GalleryImagesMaintenance"));
const StaffMaintenance = lazy(() => import("./pages/admin/StaffMaintenance"));

export default function App() {
  return (
    <Suspense fallback={<div className="page-loading">Loading...</div>}>
      <Routes>
        {/* Public site */}
        <Route element={<PublicLayout />}>
          <Route path="/" element={<Home />} />
          <Route path="/courses/:id" element={<CourseDetail />} />
          <Route path="/enquiry" element={<EnquiryForm />} />
          {/* Where Spring's OAuth2 success handler redirects back to. Inside
              PublicLayout so a visitor never leaves the site's chrome. */}
          <Route path="/oauth/callback" element={<OAuthCallback />} />
          <Route path="/campus-life" element={<CampusLife />} />
          <Route path="/campus-life/albums/:batchId" element={<BatchAlbum />} />
          <Route path="/campus-life/gallery/:category" element={<GalleryCategory />} />
          <Route path="/faculty" element={<Faculty />} />
          <Route path="/get-in-touch" element={<GetInTouch />} />
          <Route path="/placement/recruiters" element={<OurRecruiters />} />
          <Route path="/placement/recruiters/:id" element={<RecruiterDetail />} />
          <Route path="/placement/batchwise" element={<BatchwisePlacement />} />
          <Route path="/placement/batchwise/:batchId" element={<PlacementDetail />} />
        </Route>

        <Route path="/login" element={<Login />} />

        {/* Admin Panel — now genuinely gated (Day 3). The outer guard needs
            a valid token; the inner guards additionally check role, so
            Counselors get the CRM and Admin/Manager get Table Maintenance,
            matching SecurityConfig on the backend. */}
        <Route element={<ProtectedRoute />}>
          <Route path="/admin" element={<AdminLayout />}>
            {/* BRD: the Follow-up page is the admin panel's landing screen. */}
            <Route index element={<FollowupList />} />

            <Route element={<ProtectedRoute allowedRoles={CRM_ROLES} />}>
              <Route path="followups" element={<FollowupList />} />
              <Route path="inquiries" element={<Inquiries />} />
              <Route path="inquiries/new" element={<AddEnquiry />} />

              {/* Day 4 — the registration desk. Same CRM roles: registering
                  a walk-in and collecting their first installment is
                  front-desk work, not an admin-only task. */}
              <Route path="registration" element={<Registration />} />
              <Route path="students" element={<Students />} />
            </Route>

            {/* Batch and placement administration stay with Admin/Manager —
                capacity, faculty assignment and published placement stats
                aren't a counselor's call. */}
            <Route element={<ProtectedRoute allowedRoles={MASTER_DATA_ROLES} />}>
              <Route path="batches" element={<BatchManagement />} />
              <Route path="placements" element={<PlacementEntry />} />
            </Route>

            <Route element={<ProtectedRoute allowedRoles={MASTER_DATA_ROLES} />}>
              <Route path="table-maintenance/recruiters" element={<RecruitersMaintenance />} />
              <Route path="table-maintenance/announcements" element={<AnnouncementsMaintenance />} />
              <Route path="table-maintenance/closure-reasons" element={<ClosureReasonsMaintenance />} />
              <Route path="table-maintenance/course-categories" element={<CourseCategoriesMaintenance />} />
              <Route path="table-maintenance/courses" element={<CoursesMaintenance />} />
              <Route path="table-maintenance/banners" element={<BannersMaintenance />} />
              <Route path="table-maintenance/testimonials" element={<TestimonialsMaintenance />} />
              <Route path="table-maintenance/news-events" element={<NewsEventsMaintenance />} />
              <Route path="table-maintenance/gallery-images" element={<GalleryImagesMaintenance />} />
              <Route path="table-maintenance/staff" element={<StaffMaintenance />} />
            </Route>
          </Route>
        </Route>
      </Routes>
    </Suspense>
  );
}
