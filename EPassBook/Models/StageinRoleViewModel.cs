﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace EPassBook.Models
{
    public class StageinRoleViewModel
    {
        [Key]
        public int Id { get; set; }
        public Nullable<int> StageId { get; set; }
        public Nullable<int> RoleId { get; set; }

        public virtual RoleViewModel RoleMaster { get; set; }
        public virtual WorkFlowStagesViewModel WorkflowStage { get; set; }
    }
}