﻿using EPassBook.DAL.DBModel;
using EPassBook.DAL.IService;
using EPassBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using EPassBook.Helper;

namespace EPassBook.Controllers
{
    [ElmahError]
    public class WorkFlowController : Controller
    {
        IInstallmentDetailService _installmentDetailService;
        IBenificiaryService _benificiaryService;
        IWorkFlowStagesService _iWorkFlowStagesService;
        ICommentService _icommentService;
        IInstallmentRejectionService _iInstallmentRejectionService;


        public WorkFlowController(IInstallmentDetailService installmentDetailService, IBenificiaryService benificiaryService, IWorkFlowStagesService iWorkFlowStagesService, ICommentService icommentService,
           IInstallmentRejectionService iInstallmentRejectionService)
        {
            _iWorkFlowStagesService = iWorkFlowStagesService;
            _benificiaryService = benificiaryService;
            _installmentDetailService = installmentDetailService;
            _icommentService = icommentService;
            _iInstallmentRejectionService = iInstallmentRejectionService;
        }
        // GET: WorkFlow
        [HttpGet]
        [CustomAuthorize(Common.Admin, Common.SiteEngineer, Common.Accountant, Common.ChiefOfficer, Common.CityEngineer, Common.ProjectEngineer)]
        public ActionResult Index()
        {
            List<int?> stageIds;
            string strStageIds = "";
            if (Session["UserDetails"] != null)
            {
                var user = Session["UserDetails"] as UserViewModel;
                var roleId = user.UserInRoles.Select(s => s.RoleId).ToList();
                stageIds = _iWorkFlowStagesService.GetWorkflowStageById(roleId).ToList();
                strStageIds = string.Join(",", stageIds.ToArray());
            }
            var installmentListView = _installmentDetailService.GetInstallmentForLoginUsersWithStages(strStageIds).ToList();
            var resultlist = installmentListView.Select(s => new InstallmentListView
            {
                BeneficiaryId = s.BeneficiaryId,
                BeneficairyName = s.BeneficairyName,
                CompanyID = s.CompanyID,
                InstallmentId = s.InstallmentId,
                InstallmentNo = s.InstallmentNo,
                CreatedDate = Convert.ToDateTime(s.CreatedDate),
                IsCompleted = s.IsCompleted,
                MobileNo = Convert.ToString(s.MobileNo),
                PlanYear = s.PlanYear,
                StageID = s.StageID
            }).ToList();
            return View(resultlist);
        }

        [HttpGet]
        [CustomAuthorize(Common.Admin, Common.SiteEngineer, Common.Accountant, Common.ChiefOfficer, Common.CityEngineer, Common.ProjectEngineer)]
        public ActionResult Workflow(int installmentId)
        {
            var installmentDetails = _installmentDetailService.GetInstallmentDetailById(installmentId);
            //var benificiary = _benificiaryService.GetBenificiaryById(1);
            var benficiaryDetail = Mapper.BeneficiaryMapper.Detach(installmentDetails.BenificiaryMaster);
            benficiaryDetail.installmentId = installmentId;
            //Session["InstallmentId"] = id;
            string rolename = "";
            if (Session["UserDetails"] != null)
            {
                var user = Session["UserDetails"] as UserViewModel;
                var roleId = user.UserInRoles.Select(s => s.RoleId).FirstOrDefault();
                rolename = Enum.GetName(typeof(Common.WorkFlowStages), roleId);
            }
            ViewBag.RoleName = rolename;
            return View(benficiaryDetail);
        }

        [HttpGet]
        [CustomAuthorize(Common.Admin, Common.SiteEngineer, Common.Accountant, Common.ChiefOfficer, Common.CityEngineer, Common.ProjectEngineer)]
        public ActionResult Accountant(int installmentId)
        {
            AccountDetailsViewModel accountDetailsViewModel = new AccountDetailsViewModel();
            InstallmentSigning instS = new InstallmentSigning();
            var installmentDetails = _installmentDetailService.GetInstallmentDetailById(installmentId);
            var benificiaryDetails = _benificiaryService.GetBenificiaryById(installmentDetails.BeneficiaryId);

            accountDetailsViewModel.InstallmentId = installmentId;
            //Get Sign for Accountant
            if (installmentDetails.InstallmentSignings.Count > 0)
                accountDetailsViewModel.Sign = Convert.ToBoolean(installmentDetails.InstallmentSignings.Where(w => w.RoleId == (int)Common.Roles.Accountant && w.InstallmentId == installmentId).Select(s => s.Sign).FirstOrDefault());

            accountDetailsViewModel.TransactionDate = installmentDetails.TransactionDate;
            accountDetailsViewModel.TransactionId = installmentDetails.TransactionID;
            accountDetailsViewModel.LoanAmnt = Convert.ToInt32(installmentDetails.LoanAmnt);
            accountDetailsViewModel.IFSCCode = benificiaryDetails.IFSCCode;
            accountDetailsViewModel.AccountNo = benificiaryDetails.AccountNo.ToString();
            accountDetailsViewModel.LoanAmtInRupees = accountDetailsViewModel.LoanAmnt.ConvertNumbertoWords();
            accountDetailsViewModel.TransactionType = installmentDetails.TransactionType;
            return PartialView("_Accountant", accountDetailsViewModel);
        }

        [HttpPost]
        [CustomAuthorize(Common.Accountant)]
        public ActionResult Accountant(AccountDetailsViewModel accountDetailsVM)
        {
            var installmentDetail = _installmentDetailService.GetInstallmentDetailById(accountDetailsVM.InstallmentId);
            
            if (Session["UserDetails"] != null)
            {
                var user = Session["UserDetails"] as UserViewModel;
                var instSigning = new InstallmentSigning();

                instSigning.InstallmentId = installmentDetail.InstallmentId;
                instSigning.Sign = accountDetailsVM.Sign;
                instSigning.UserId = user.UserId;
                instSigning.RoleId = (int)Common.Roles.Accountant;
                instSigning.CreatedDate = DateTime.Now;
                instSigning.CreatedBy = user.UserName;
                instSigning.CompanyID = user.CompanyID;

                installmentDetail.TransactionID = accountDetailsVM.TransactionId;
                installmentDetail.ModifiedBy = user.UserName;
                installmentDetail.ModifiedDate = DateTime.Now;
                installmentDetail.StageID = (int)Common.WorkFlowStages.Accountant;
                installmentDetail.TransactionDate = accountDetailsVM.TransactionDate;
                installmentDetail.TransactionType = accountDetailsVM.TransactionType;

                if (ModelState.IsValid)
                {
                    installmentDetail.InstallmentSignings.Add(instSigning);
                    _installmentDetailService.Update(installmentDetail);
                    _installmentDetailService.SaveChanges();
                    ViewBag.Message = "sussess message";
                    return View("_Accountant", accountDetailsVM);
                }
                else
                {
                    var benificiaryDetails = _benificiaryService.GetBenificiaryById(installmentDetail.BeneficiaryId);
                    accountDetailsVM.InstallmentId = Convert.ToInt32(accountDetailsVM.InstallmentId);
                    accountDetailsVM.LoanAmnt = Convert.ToInt32(installmentDetail.LoanAmnt);
                    accountDetailsVM.IFSCCode = benificiaryDetails.IFSCCode;
                    accountDetailsVM.AccountNo = benificiaryDetails.AccountNo.ToString();
                    accountDetailsVM.LoanAmtInRupees = accountDetailsVM.LoanAmnt.ConvertNumbertoWords();

                    return View("_Accountant", accountDetailsVM);
                }
            }
            else
            {
                return RedirectToAction("Login", "User");
            }
        }

        [HttpGet]
        [CustomAuthorize(Common.Admin, Common.SiteEngineer, Common.Accountant, Common.ChiefOfficer, Common.CityEngineer, Common.ProjectEngineer)]
        public ActionResult Recommend()
        {
            return View();
        }

        [HttpPost]
        [CustomAuthorize(Common.SiteEngineer)]
        public ActionResult Recommend(InstallmentDetailsViewModel installmentDetailViewModel)
        {
            if (ModelState.IsValidField("txtFirstComment"))
            {
                if (Session["UserDetails"] != null)
                {
                    var installment = _installmentDetailService.GetInstallmentDetailById(installmentDetailViewModel.InstallmentId);

                    var comments = new Comment();
                    var user = Session["UserDetails"] as UserViewModel;
                    comments.Comments = installmentDetailViewModel.FirstComment;
                    comments.CreatedBy = user.UserName;
                    comments.BeneficiaryId = installment.BeneficiaryId;
                    comments.CreatedDate = DateTime.Now;
                    comments.CompanyID = user.CompanyID;
                    comments.RoleId = user.UserInRoles.Where(u => u.UserId == user.UserId).Select(r => r.RoleId).FirstOrDefault();
                    installment.IsRecommended = true;
                    installment.ModifiedBy = user.UserName;
                    installment.ModifiedDate = DateTime.Now;
                    installment.Comments.Add(comments);
                    _installmentDetailService.Update(installment);
                    _installmentDetailService.SaveChanges();
                    return Json(installmentDetailViewModel.InstallmentId); //("SiteEngineer", "WorkFlow", installmentDetailViewModel.InstallmentId);
                }
            }
            return Json(installmentDetailViewModel.InstallmentId);
        }

        [HttpPost]
        [CustomAuthorize(Common.SiteEngineer)]
        public ActionResult Reject(InstallmentDetailsViewModel installmentDetailViewModel)
        {
            if (ModelState.IsValidField("txtFirstComment"))
            {
                if (Session["UserDetails"] != null)
                {
                    var installment = _installmentDetailService.GetInstallmentDetailById(installmentDetailViewModel.InstallmentId);

                    var comments = new Comment();
                    var installmentRejection = new InstallmentRejection();
                    var user = Session["UserDetails"] as UserViewModel;

                    //add data into comments table
                    comments.Comments = installmentDetailViewModel.FirstComment;
                    comments.CreatedBy = user.UserName;
                    comments.BeneficiaryId = installment.BeneficiaryId;
                    comments.CreatedDate = DateTime.Now;
                    comments.CompanyID = user.CompanyID;
                    comments.RoleId = user.UserInRoles.Where(u => u.UserId == user.UserId).Select(r => r.RoleId).FirstOrDefault();

                    //add data into installmentRejection table
                    installmentRejection.BeneficiaryId = installment.BeneficiaryId;
                    installmentRejection.InstallmentNo = installment.InstallmentNo;
                    installmentRejection.Comment = installmentDetailViewModel.FirstComment;
                    installmentRejection.CreatedDate = DateTime.Now;
                    installmentRejection.CreatedBy = user.UserName;
                    installmentRejection.CompanyID = user.CompanyID;

                    //add data into installmentDetails table
                    installment.IsRecommended = false;
                    installment.StageID = Convert.ToInt32(Common.WorkFlowStages.Rejected);
                    installment.ModifiedBy = user.UserName;
                    installment.ModifiedDate = DateTime.Now;
                    _iInstallmentRejectionService.Add(installmentRejection);
                    _iInstallmentRejectionService.SaveChanges();
                    installment.Comments.Add(comments);
                    _installmentDetailService.SaveChanges();
                    return Json(installmentDetailViewModel.InstallmentId);
                }
            }
            return Json(installmentDetailViewModel.InstallmentId);
        }


        [HttpGet]
        [CustomAuthorize(Common.Admin, Common.SiteEngineer, Common.Accountant, Common.ChiefOfficer, Common.CityEngineer, Common.ProjectEngineer)]
        public ActionResult SiteEngineer(int installmentId)
        {
            var installment = _installmentDetailService.GetInstallmentDetailById(installmentId);
            var installmentviewmodel = Mapper.InstallmentDetailsMapper.Detach(installment);

            //Get Comment for Project Engineer
            if (installmentviewmodel.Comments.Count > 0)
                installmentviewmodel._Comments = installment.Comments.Where(w => w.RoleId == (int)Common.Roles.SiteEngineer && w.InstallementId == installmentId).Select(s => s.Comments).FirstOrDefault();

            //Get Sign for Project Engineer
            if (installmentviewmodel.InstallmentSignings.Count > 0)
                installmentviewmodel.Sign = Convert.ToBoolean(installment.InstallmentSignings.Where(w => w.RoleId == (int)Common.Roles.SiteEngineer && w.InstallmentId == installmentId).Select(s => s.Sign).FirstOrDefault());
            //Get Geo Tagging Photo
            if (installmentviewmodel.GeoTaggingDetails.Count > 0)
                installmentviewmodel.Photo = "/Uploads/SiteEngPhotos/" + installmentviewmodel.GeoTaggingDetails.FirstOrDefault().Photo;

            installmentviewmodel.lInRupees = Convert.ToInt64(installmentviewmodel.LoanAmnt).ConvertNumbertoWords();
            installmentviewmodel.beniInRupees = Convert.ToInt64(installmentviewmodel.BeneficiaryAmnt).ConvertNumbertoWords();
            installmentviewmodel.TransactionType = installment.TransactionType;

            if (installmentviewmodel.lInRupees == "ZERO")
            {
                installmentviewmodel.lInRupees = null;
            }
            if (installmentviewmodel.beniInRupees == "ZERO")
            {
                installmentviewmodel.beniInRupees = null;
            }
            return PartialView("_SiteEngineer", installmentviewmodel);
        }

        [HttpPost]
        [CustomAuthorize(Common.SiteEngineer)]
        public ActionResult SiteEngineer(InstallmentDetailsViewModel installmentDetailViewModel)
        {
            HttpPostedFileBase hasbandphoto = Request.Files["imguploadsiteeng"];

            string photourl = PhotoManager.savePhoto(hasbandphoto, installmentDetailViewModel.InstallmentId, "SiteEngineer");

            var installment = _installmentDetailService.GetInstallmentDetailById(installmentDetailViewModel.InstallmentId);
            if (ModelState.IsValid)
            {
                //if (installment.OTP != installmentDetailViewModel.OTP)
                //{
                //    ViewBag.Error = "Wrong OTP";
                //    //return RedirectToAction("Workflow", "Workflow", installmentDetailViewModel.InstallmentId);
                //    return PartialView("_SiteEngineer", installmentDetailViewModel);
                //}
                if (Session["UserDetails"] != null)
                {
                    var user = Session["UserDetails"] as UserViewModel;
                    installment.ModifiedBy = user.UserName;
                    installment.CompanyID = user.CompanyID;

                    installment.BeneficiaryAmnt = installmentDetailViewModel.BeneficiaryAmnt;
                    installment.LoanAmnt = installmentDetailViewModel.LoanAmnt;
                    installment.IsCentreAmnt = installmentDetailViewModel.IsCentreAmnt;
                    installment.ConstructionLevel = installmentDetailViewModel.ConstructionLevel;
                    installment.StageID = (int)Common.WorkFlowStages.SiteEngineer;
                    installment.InstallmentNo = installment.InstallmentNo;
                    installment.ModifiedDate = DateTime.Now;
                    installment.TransactionType = installmentDetailViewModel.TransactionType;

                    // Insert reocrd in comment table 
                    var comments = new Comment();
                    comments.Comments = installmentDetailViewModel._Comments;
                    comments.CreatedBy = user.UserName;
                    comments.BeneficiaryId = installment.BeneficiaryId;
                    comments.RoleId = (int)Common.Roles.SiteEngineer;
                    comments.CreatedDate = DateTime.Now;
                    comments.CompanyID = user.CompanyID;

                    // Insert reocrd in GeoTaggingDetail table 
                    var geotaging = new GeoTaggingDetail();
                    geotaging.BeneficiaryId = installment.BeneficiaryId;
                    geotaging.CompanyID = user.CompanyID;
                    geotaging.ConstructionLevel = installmentDetailViewModel.ConstructionLevel;
                    geotaging.UserId = user.UserId;
                    geotaging.CreatedBy = user.UserName;
                    geotaging.CreatedDate = DateTime.Now;

                    if (photourl != "empty" && photourl != "fail")
                    {
                        geotaging.Photo = photourl;
                    }

                    // Insert reocrd in GeoTaggingDetail table 
                    var signing = new InstallmentSigning();
                    signing.InstallmentId = installment.InstallmentId;
                    signing.UserId = user.UserId;
                    signing.RoleId = (int)Common.Roles.SiteEngineer;
                    signing.Sign = true;
                    signing.CreatedDate = DateTime.Now;
                    signing.CreatedBy = user.UserName;
                    signing.CompanyID = user.CompanyID;

                    // Applying changes to database tables
                    installment.Comments.Add(comments);
                    installment.GeoTaggingDetails.Add(geotaging);
                    installment.InstallmentSignings.Add(signing);
                    _installmentDetailService.Update(installment);

                    _installmentDetailService.SaveChanges();

                    ViewBag.Message = "sussess message";
                }
            }

            return PartialView("_SiteEngineer", installmentDetailViewModel);
        }

        [HttpGet]
        [CustomAuthorize(Common.Admin, Common.SiteEngineer, Common.Accountant, Common.ChiefOfficer, Common.CityEngineer, Common.ProjectEngineer)]
        public ActionResult SurveyDetails(int installmentId)
        {
            var installmentNo = _installmentDetailService.Get().Where(i => i.InstallmentId == installmentId).Select(x => x.InstallmentNo).FirstOrDefault();
            var BeniId = _icommentService.Get().Where(i => i.InstallementId == installmentId).Select(b => b.BeneficiaryId).FirstOrDefault();
            IEnumerable<sp_GetSurveyDetailsByBenID_Result> commentlist = _icommentService.GetSurveyDetailsByBenificiaryID(BeniId, installmentNo.Value);

            var mappedCommentList = commentlist.Select(s => new SurveyDetailsModel
            {
                BeneficiaryId = s.BeneficiaryId,
                Comments = s.Comments,
                UserName = s.UserName,
                CreatedDate = s.CreatedDate,
                MobileNo = s.MobileNo,
                Sign = Convert.ToBoolean(s.Sign),
                Physical_Progress = s.Physical_Progress,
                Role = s.Role

            }).ToList();

            return PartialView("_SurveyDetails", mappedCommentList);

        }

        [HttpGet]
        [CustomAuthorize(Common.Admin, Common.SiteEngineer, Common.Accountant, Common.ChiefOfficer, Common.CityEngineer, Common.ProjectEngineer)]
        public ActionResult DataEntry(int installmentId)
        {
            int beneficiaryId = 0;
            var installment = _installmentDetailService.GetInstallmentDetailById(installmentId);
            if (installment != null)
            {
                beneficiaryId = installment.BeneficiaryId;
            }
            var beneficiary = _benificiaryService.GetBenificiaryById(beneficiaryId);
            var beneficiaryvm = Mapper.BeneficiaryMapper.Detach(beneficiary);
            beneficiaryvm.Hasband_Photo = "/Uploads/BeneficiaryImages/" + beneficiaryvm.Hasband_Photo;
            beneficiaryvm.Wife_Photo = "/Uploads/BeneficiaryImages/" + beneficiaryvm.Wife_Photo;

            return PartialView("_DataEntry", beneficiaryvm);
        }

        [HttpGet]
        [CustomAuthorize(Common.Admin, Common.SiteEngineer, Common.Accountant, Common.ChiefOfficer, Common.CityEngineer, Common.ProjectEngineer)]
        public ActionResult ProjectEngineer(int installmentId)
        {
            var userdetails = new UserViewModel();
            if (Session["UserDetails"] != null)
            {
                userdetails = Session["UserDetails"] as UserViewModel;
            }

            var installment = _installmentDetailService.GetInstallmentDetailById(installmentId);
            var installmentviewmodel = Mapper.InstallmentDetailsMapper.Detach(installment);

            //Get Comment for Project Engineer
            if (installmentviewmodel.Comments.Count > 0)
                installmentviewmodel._Comments = installment.Comments.Where(w => w.RoleId == (int)Common.Roles.ProjectEngineer && w.InstallementId == installmentId).Select(s => s.Comments).FirstOrDefault();

            //Get Sign for Project Engineer
            if (installmentviewmodel.InstallmentSignings.Count > 0)
                installmentviewmodel.Sign = Convert.ToBoolean(installment.InstallmentSignings.Where(w => w.RoleId == (int)Common.Roles.ProjectEngineer && w.InstallmentId == installmentId).Select(s => s.Sign).FirstOrDefault());

            installmentviewmodel.lInRupees = Convert.ToInt64(installmentviewmodel.LoanAmnt).ConvertNumbertoWords();
            installmentviewmodel.beniInRupees = Convert.ToInt64(installmentviewmodel.BeneficiaryAmnt).ConvertNumbertoWords();
            if (installmentviewmodel.GeoTaggingDetails.Count > 0)
                installmentviewmodel.Photo = "/Uploads/SiteEngPhotos/" + installmentviewmodel.GeoTaggingDetails.FirstOrDefault().Photo;
            installmentviewmodel.TransactionType = installment.TransactionType;

            if (installmentviewmodel.lInRupees == "ZERO")
            {
                installmentviewmodel.lInRupees = null;
            }
            if (installmentviewmodel.beniInRupees == "ZERO")
            {
                installmentviewmodel.beniInRupees = null;
            }
            return PartialView("_ProjectEngineer", installmentviewmodel);
        }

        [HttpPost]
        [CustomAuthorize(Common.ProjectEngineer)]
        public ActionResult ProjectEngineer(InstallmentDetailsViewModel installmentDetailViewModel)
        {
            var installment = _installmentDetailService.GetInstallmentDetailById(installmentDetailViewModel.InstallmentId);
            
            if (Session["UserDetails"] != null)
            {
                var user = Session["UserDetails"] as UserViewModel;
                installment.ModifiedBy = user.UserName;
                installment.StageID = (int)Common.WorkFlowStages.ProjectEngineer;
                installment.ModifiedDate = DateTime.Now;

                // Insert reocrd in comment table 
                var comments = new Comment();
                comments.Comments = installmentDetailViewModel._Comments;
                comments.CreatedBy = user.UserName;
                comments.BeneficiaryId = installment.BeneficiaryId;
                comments.RoleId = (int)Common.Roles.ProjectEngineer;
                comments.CreatedDate = DateTime.Now;
                comments.CompanyID = user.CompanyID;

                // Insert reocrd in GeoTaggingDetail table 
                var signing = new InstallmentSigning();
                signing.InstallmentId = installmentDetailViewModel.InstallmentId;
                signing.UserId = user.UserId;
                signing.RoleId = (int)Common.Roles.ProjectEngineer;
                signing.Sign = true;
                signing.CreatedDate = DateTime.Now;
                signing.CreatedBy = user.UserName;
                signing.CompanyID = user.CompanyID;

                // Applying changes to database tables
                installment.Comments.Add(comments);

                installment.InstallmentSignings.Add(signing);
                _installmentDetailService.Update(installment);


                _installmentDetailService.SaveChanges();

                ViewBag.Message = "sussess message";

                installmentDetailViewModel.BeneficiaryAmnt = installment.BeneficiaryAmnt;
                installmentDetailViewModel.LoanAmnt = installment.LoanAmnt;
                installmentDetailViewModel.ConstructionLevel = installment.ConstructionLevel;
            }
            return PartialView("_ProjectEngineer", installmentDetailViewModel);
        }

        [HttpGet]
        [CustomAuthorize(Common.Admin, Common.SiteEngineer, Common.Accountant, Common.ChiefOfficer, Common.CityEngineer, Common.ProjectEngineer)]
        public ActionResult CityEngineer(int installmentId)
        {
            var installment = _installmentDetailService.GetInstallmentDetailById(installmentId);
            var installmentviewmodel = Mapper.InstallmentDetailsMapper.Detach(installment);
            //Get Comment for City Engineer
            if (installmentviewmodel.Comments.Count > 0)
                installmentviewmodel._Comments = installment.Comments.Where(w => w.RoleId == (int)Common.Roles.CityEngineer && w.InstallementId == installmentId).Select(s => s.Comments).FirstOrDefault();

            //Get Sign for City Engineer
            if (installmentviewmodel.InstallmentSignings.Count > 0)
                installmentviewmodel.Sign = Convert.ToBoolean(installment.InstallmentSignings.Where(w => w.RoleId == (int)Common.Roles.CityEngineer && w.InstallmentId == installmentId).Select(s => s.Sign).FirstOrDefault());
            installmentviewmodel.lInRupees = Convert.ToInt64(installmentviewmodel.LoanAmnt).ConvertNumbertoWords();
            installmentviewmodel.beniInRupees = Convert.ToInt64(installmentviewmodel.BeneficiaryAmnt).ConvertNumbertoWords();
            installmentviewmodel.TransactionType = installment.TransactionType;

            if (installmentviewmodel.GeoTaggingDetails.Count > 0)
                installmentviewmodel.Photo = "/Uploads/SiteEngPhotos/" + installmentviewmodel.GeoTaggingDetails.FirstOrDefault().Photo;

            if (installmentviewmodel.lInRupees == "ZERO")
            {
                installmentviewmodel.lInRupees = null;
            }
            if (installmentviewmodel.beniInRupees == "ZERO")
            {
                installmentviewmodel.beniInRupees = null;
            }
            return PartialView("_CityEngineer", installmentviewmodel);
        }

        [HttpPost]
        [CustomAuthorize(Common.CityEngineer)]
        public ActionResult CityEngineer(InstallmentDetailsViewModel installmentDetailViewModel)
        {
            var installment = _installmentDetailService.GetInstallmentDetailById(installmentDetailViewModel.InstallmentId);
            
            //if (ModelState.IsValid)
            //{
            if (Session["UserDetails"] != null)
            {
                var user = Session["UserDetails"] as UserViewModel;
                installment.ModifiedBy = user.UserName;
                installment.StageID = (int)Common.WorkFlowStages.CityEngineer;
                installment.ModifiedDate = DateTime.Now;

                // Insert reocrd in comment table 
                var comments = new Comment();
                comments.Comments = installmentDetailViewModel._Comments;
                comments.CreatedBy = user.UserName;
                comments.BeneficiaryId = installment.BeneficiaryId;
                comments.RoleId = (int)Common.Roles.CityEngineer;
                comments.CreatedDate = DateTime.Now;
                comments.CompanyID = user.CompanyID;

                // Insert reocrd in GeoTaggingDetail table 
                var signing = new InstallmentSigning();
                signing.InstallmentId = installmentDetailViewModel.InstallmentId;
                signing.UserId = user.UserId;
                signing.RoleId = (int)Common.Roles.CityEngineer;//user.UserInRoles.FirstOrDefault().RoleId;
                signing.Sign = true;
                signing.CreatedDate = DateTime.Now;
                signing.CreatedBy = user.UserName;
                signing.CompanyID = user.CompanyID;

                // Applying changes to database tables
                installment.Comments.Add(comments);

                installment.InstallmentSignings.Add(signing);
                _installmentDetailService.Update(installment);


                _installmentDetailService.SaveChanges();

                Session["InstallmentId"] = null;
                ViewBag.Message = "sussess message";

                installmentDetailViewModel.BeneficiaryAmnt = installment.BeneficiaryAmnt;
                installmentDetailViewModel.LoanAmnt = installment.LoanAmnt;
                installmentDetailViewModel.ConstructionLevel = installment.ConstructionLevel;

                //return RedirectToAction("Index", "WorkFlow");
            }
            //}

            return PartialView("_CityEngineer", installmentDetailViewModel);
        }

        [HttpGet]
        [CustomAuthorize(Common.Admin, Common.SiteEngineer, Common.Accountant, Common.ChiefOfficer, Common.CityEngineer, Common.ProjectEngineer)]
        public ActionResult ChiefOfficer(int installmentId)
        {
            var installment = _installmentDetailService.GetInstallmentDetailById(installmentId);
            var installmentviewmodel = Mapper.InstallmentDetailsMapper.Detach(installment);
            //Get Comment for Chief Officer
            if (installmentviewmodel.Comments.Count > 0)
                installmentviewmodel._Comments = installment.Comments.Where(w => w.RoleId == (int)Common.Roles.ChiefOfficer && w.InstallementId == installmentId).Select(s => s.Comments).FirstOrDefault();

            //Get Sign for Chief Officer
            if (installmentviewmodel.InstallmentSignings.Count > 0)
                installmentviewmodel.Sign = Convert.ToBoolean(installment.InstallmentSignings.Where(w => w.RoleId == (int)Common.Roles.ChiefOfficer && w.InstallmentId == installmentId).Select(s => s.Sign).FirstOrDefault());

            //Get Geotagging photo
            if (installmentviewmodel.GeoTaggingDetails.Count > 0)
                installmentviewmodel.Photo = "/Uploads/SiteEngPhotos/" + installmentviewmodel.GeoTaggingDetails.FirstOrDefault().Photo;
            installmentviewmodel.TransactionType = installment.TransactionType;

            installmentviewmodel.lInRupees = Convert.ToInt64(installmentviewmodel.LoanAmnt).ConvertNumbertoWords();
            installmentviewmodel.beniInRupees = Convert.ToInt64(installmentviewmodel.BeneficiaryAmnt).ConvertNumbertoWords();

            if (installmentviewmodel.lInRupees == "ZERO")
            {
                installmentviewmodel.lInRupees = null;
            }
            if (installmentviewmodel.beniInRupees == "ZERO")
            {
                installmentviewmodel.beniInRupees = null;
            }

            return PartialView("_ChiefOfficer", installmentviewmodel);
        }

        [HttpPost]
        [CustomAuthorize(Common.ChiefOfficer)]
        public ActionResult ChiefOfficer(InstallmentDetailsViewModel installmentDetailViewModel)
        {
            var installment = _installmentDetailService.GetInstallmentDetailById(installmentDetailViewModel.InstallmentId);
            
            if (installment != null)
            {
                if (Session["UserDetails"] != null)
                {
                    var user = Session["UserDetails"] as UserViewModel;

                    if (installment.StageID == (int)Common.WorkFlowStages.Accountant)
                    {
                        installment.IsCompleted = true;
                        installment.StageID = (int)Common.WorkFlowStages.LastChiefOfficer;

                        InstallmentDetail installmentDetail = new InstallmentDetail();
                        //Insert new rocrd with new installment in installment details
                        var newInstallmentNo = installment.InstallmentNo;
                        if (newInstallmentNo <= 6)
                        {
                            newInstallmentNo = newInstallmentNo + 1;
                            installmentDetail.InstallmentNo = newInstallmentNo;
                            installmentDetail.BeneficiaryId = installment.BeneficiaryId;
                            installmentDetail.BeneficiaryAmnt = 0;
                            installmentDetail.LoanAmnt = 0;
                            installmentDetail.IsCentreAmnt = null;
                            installmentDetail.StageID = 1;
                            installmentDetail.IsCompleted = false;
                            installmentDetail.CreatedDate = DateTime.Now;
                            installmentDetail.CreatedBy = "System";
                            installmentDetail.CompanyID = installment.CompanyID;
                            installmentDetail.IsRecommended = false;

                            _installmentDetailService.Add(installmentDetail);
                            _installmentDetailService.SaveChanges();
                        }
                    }
                    else
                    {
                        installment.StageID = (int)Common.WorkFlowStages.ChiefOfficer;
                    }
                    installment.ModifiedBy = user.UserName;
                    installment.ModifiedDate = DateTime.Now;

                    // Insert reocrd in comment table 
                    var comments = new Comment();
                    comments.Comments = installmentDetailViewModel._Comments;
                    comments.CreatedBy = user.UserName;
                    comments.BeneficiaryId = installment.BeneficiaryId;
                    comments.RoleId = (int)Common.Roles.ChiefOfficer;
                    comments.CreatedDate = DateTime.Now;
                    comments.CompanyID = user.CompanyID;

                    // Insert reocrd in GeoTaggingDetail table 
                    var signing = new InstallmentSigning();
                    signing.InstallmentId = installment.InstallmentId;
                    signing.UserId = user.UserId;
                    signing.RoleId = (int)Common.Roles.ChiefOfficer;
                    signing.Sign = true;
                    signing.CreatedDate = DateTime.Now;
                    signing.CreatedBy = user.UserName;
                    signing.CompanyID = user.CompanyID;

                    // Applying changes to database tables
                    installment.Comments.Add(comments);

                    installment.InstallmentSignings.Add(signing);
                    _installmentDetailService.Update(installment);


                    _installmentDetailService.SaveChanges();

                    ViewBag.Message = "sussess message";

                    installmentDetailViewModel.BeneficiaryAmnt = installment.BeneficiaryAmnt;
                    installmentDetailViewModel.LoanAmnt = installment.LoanAmnt;
                    installmentDetailViewModel.ConstructionLevel = installment.ConstructionLevel;
                }
            }

            return PartialView("_ChiefOfficer", installmentDetailViewModel);
        }

        [HttpGet]
        [CustomAuthorize(Common.Admin, Common.SiteEngineer, Common.Accountant, Common.ChiefOfficer, Common.CityEngineer, Common.ProjectEngineer)]
        public ActionResult WorkStatusDetails(int InstallmentId)
        {
            //since we want all records of installment, so we use same name of intallment as parameter.
            int beneficiaryId = 0;
            var installment = _installmentDetailService.GetInstallmentDetailById(InstallmentId);
            if (installment != null)
            {
                beneficiaryId = installment.BeneficiaryId;
            }

            List<WorkStatusDetailsViewModel> workstatus = new List<WorkStatusDetailsViewModel>();
            var installments = _installmentDetailService.Get(w => w.BeneficiaryId == beneficiaryId, null, "").ToList();

            if (installments != null)
            {
                workstatus = installments.Select(s => new WorkStatusDetailsViewModel
                {
                    Installment = s.InstallmentNo == 1 ? "First" : s.InstallmentNo == 2 ? "Second" : s.InstallmentNo == 3 ? "Thir" : s.InstallmentNo == 4 ? "Fourth" : s.InstallmentNo == 5 ? "Fifth" : s.InstallmentNo == 6 ? "Sixth-Cum Final" : "",
                    LevelType = s.ConstructionLevel,
                    BeneficiaryAmount = s.BeneficiaryAmnt == null ? 0 : s.BeneficiaryAmnt,
                    CenterAmount = s.IsCentreAmnt == null ? 0 : s.IsCentreAmnt == true ? s.LoanAmnt : 0,
                    StateAmount = s.IsCentreAmnt == null ? 0 : s.IsCentreAmnt == false ? s.LoanAmnt : 0,
                    ULBAmount = 0,
                    TotalAmount = s.LoanAmnt,
                }).ToList();

                ViewBag.GrandTotal = workstatus.Sum(w => w.TotalAmount);
            }

            return PartialView("_WorkStatusDetails", workstatus);
        }

        [HttpPost]
        public string ToWords(long number)
        {
            var Rupees = number.ConvertNumbertoWords();
            return Rupees;
        }

        [HttpGet]
        public string SendOtp(int installmentID)
        {
            var uvm = Session["UserDetails"] as UserViewModel;
            VerifyUser verifyUser = new VerifyUser();
            verifyUser.msisdn = uvm.MobileNo;
            verifyUser.OTP = verifyUser.GenerateRandomOTP(4);

            string res = verifyUser.SendOtp();

            //save otp to DB;
            InstallmentDetail installmentDetail = new InstallmentDetail();
            installmentDetail = _installmentDetailService.Get().Where(i => i.InstallmentId == installmentID).FirstOrDefault();
            installmentDetail.OTP = verifyUser.OTP;
            _installmentDetailService.Update(installmentDetail);
            _installmentDetailService.SaveChanges();

            return res;
        }

        [HttpGet]
        public bool ValidateOtp(int installmentId, int Otp)
        {
            var isVerified = false;
            if (!string.IsNullOrEmpty(Convert.ToString(installmentId)) && !string.IsNullOrEmpty(Convert.ToString(installmentId)))
            {
                var installment = _installmentDetailService.GetInstallmentDetailById(installmentId);
                var DBotp = installment.OTP;
                if (!string.IsNullOrEmpty(DBotp))
                {
                    if (Otp == Convert.ToInt32(DBotp))
                    {
                        isVerified = true;
                    }
                }
            }
            return isVerified;
        }
    }
}