﻿using EPassBook.DAL.DBModel;
using EPassBook.DAL;
using EPassBook.DAL.IService;
using EPassBook.DAL.Repository;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace EPassBook.DAL.Service
{
    public class CommentService : ICommentService
    {
        private readonly epassbook_dbEntities _dbContext;
        private UnitOfWork unitOfWork;
        private GenericRepository<Comment> commentRepository;

        public CommentService()
        {
            _dbContext = new epassbook_dbEntities();
            unitOfWork = new UnitOfWork(_dbContext);
            commentRepository = unitOfWork.GenericRepository<Comment>();
        }

        public IEnumerable<Comment> Get(Expression<Func<Comment, bool>> filter = null,
          Func<IQueryable<Comment>, IOrderedQueryable<Comment>> orderBy = null,
          string includeProperties = "")
        {
            IEnumerable<Comment> comments = commentRepository.Get(filter, orderBy, includeProperties).ToList();
            return comments;
        }
        public IEnumerable<Comment> GetAllComments()
        {
            IEnumerable<Comment> comment = commentRepository.GetAll().ToList();
            return comment;
        }
        public Comment GetCommentById(int id)
        {
            Comment comment = commentRepository.GetById(id);
            return comment;
        }

        public void Add(Comment comment)
        {
            commentRepository.Add(comment);
        }
        public void Update(Comment comment)
        {
            commentRepository.Update(comment);
        }
        public void Delete(int id)
        {
            commentRepository.Delete(id);
        }

        public void SaveChanges()
        {
            unitOfWork.SaveChanges();
        }

        IEnumerable<sp_GetSurveyDetailsByBenID_Result> ICommentService.GetSurveyDetailsByBenificiaryID(int id,int installmentNo)
        {
            var surveyDetails = _dbContext.sp_GetSurveyDetailsByBenID(id,installmentNo);    
            //parameter added for testing only
            return surveyDetails.ToList();
        }
    }
}
