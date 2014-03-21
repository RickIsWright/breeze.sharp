﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Linq;
using Breeze.Core;
using Breeze.Sharp;
using System.Collections.Generic;
using Foo;
using Inheritance.Models;
using System.Collections;

namespace Test_NetClient {

  [TestClass]
  public class InheritanceTests {

    private String _serviceName;

    [TestInitialize]
    public void TestInitializeMethod() {
      MetadataStore.Instance.ProbeAssemblies(typeof(BillingDetailTPC).Assembly);
      _serviceName = "http://localhost:7150/breeze/Inheritance/";
    }

    [TestCleanup]
    public void TearDown() {
      
    }

    [TestMethod]
    public async Task SimpleTPH() {
      var em1 = await TestFns.NewEm(_serviceName);
      var r = await QueryBillingBase<BillingDetailTPH>(em1, "BillingDetailTPH");
    }

    [TestMethod]
    public async Task SimpleBillingDetailTPT() {
      var em1 = await TestFns.NewEm(_serviceName);
      var r = await QueryBillingBase<BillingDetailTPT>(em1, "BillingDetailTPT");
    }

    [TestMethod]
    public async Task SimpleBillingDetailTPC() {
      var em1 = await TestFns.NewEm(_serviceName);
      var r = await QueryBillingBase<BillingDetailTPC>(em1, "BillingDetailTPC");
    }

    [TestMethod]
    public async Task SimpleBillingDetailTPH() {
      var em1 = await TestFns.NewEm(_serviceName);
      var r = await QueryBillingBase<BillingDetailTPH>(em1, "BillingDetailTPH");
    }

    [TestMethod]
    public async Task SimpleBankAccountTPT() {
      var em1 = await TestFns.NewEm(_serviceName);
      var r = await QueryBillingBase<BankAccountTPT>(em1, "BankAccountTPT");
    }

    [TestMethod]
    public async Task SimpleBankAccountTPC() {
      var em1 = await TestFns.NewEm(_serviceName);
      var r = await QueryBillingBase<BankAccountTPC>(em1, "BankAccountTPC");
    }

    [TestMethod]
    public async Task SimpleBankAccountTPH() {
      var em1 = await TestFns.NewEm(_serviceName);
      var r = await QueryBillingBase<BankAccountTPH>(em1, "BankAccountTPH");
    }

    [TestMethod]
    public async Task CanDeleteBankAccountTPT() {
      var em1 = await TestFns.NewEm(_serviceName);
      var r = await CanDeleteBillingBase<BankAccountTPT>(em1, "BankAccountTPT", "Deposits");
    }

    [TestMethod]
    public async Task CanDeleteSimpleBankAccountTPC() {
      var em1 = await TestFns.NewEm(_serviceName);
      var r = await CanDeleteBillingBase<BankAccountTPC>(em1, "BankAccountTPC", "Deposits");
    }

    [TestMethod]
    public async Task CanDeleteSimpleBankAccountTPH() {
      var em1 = await TestFns.NewEm(_serviceName);
      var r = await CanDeleteBillingBase<BankAccountTPH>(em1, "BankAccountTPH", "Deposits");
    }

    [TestMethod]
    public async Task CreateAndSaveBankAccoutTPT() {
      var em1 = await TestFns.NewEm(_serviceName);
      var r = await CreateAndSaveBillingDetail<BankAccountTPT>(em1);
    }

    [TestMethod]
    public async Task CreateAndSaveBankAccountTPC() {
      var em1 = await TestFns.NewEm(_serviceName);
      var r = await CreateAndSaveBillingDetail<BankAccountTPC>(em1);
    }

    [TestMethod]
    public async Task CreateAndSaveBankAccountTPH() {
      var em1 = await TestFns.NewEm(_serviceName);
      var r = await CreateAndSaveBillingDetail<BankAccountTPH>(em1);
    }

    private async Task<IEnumerable<T>> QueryBillingBase<T>(EntityManager em, String typeName) where T: IBillingDetail {
      var q0 = new EntityQuery<T>(typeName + "s").With(em);
      var r0 = await q0.Execute();
      if (r0.Count() == 0) {
        Assert.Inconclusive("Please restart the server - inheritance data was deleted by prior tests");
      }
      
      Assert.IsTrue(r0.All(r => typeof(T).IsAssignableFrom(r.GetType())));
      Assert.IsTrue(r0.All(r => r.Owner == r.Owner.ToUpper()), "all owners should be uppercase (from initializer)");
      Assert.IsTrue(r0.All(r => r.MiscData == "asdf"), "all 'MiscData' should be 'asdf' (from initializer)");
      return r0;
    }

    private async Task<IEnumerable<T>> CanDeleteBillingBase<T>(EntityManager em, String typeName, String expandPropName = null) where T:IEntity {
      //    // Registering resource names for each derived type
      //    // because these resource names are not in metadata
      //    // because there are no corresponding DbSets in the DbContext
      //    // and that's how Breeze generates resource names
      MetadataStore.Instance.AddResourceName(typeName + "s", typeof(T));
      var q0 = new EntityQuery<T>(typeName + "s").With(em).Take(1);
      if (expandPropName != null) {
        q0 = q0.Expand(expandPropName);
      }
      var r0 = await q0.Execute();
      if (r0.Count() == 0) {
        Assert.Inconclusive("Please restart the server - inheritance data was deleted by prior tests");
      }
      var targetEntity = r0.First();
      var targetKey = targetEntity.EntityAspect.EntityKey;
      List<IEntity> dependentEntities = null;
      if (expandPropName != null) {
        var expandVal = (IEnumerable)targetEntity.EntityAspect.GetValue(expandPropName);
        dependentEntities = expandVal.Cast<IEntity>().ToList();
        dependentEntities.ForEach(de => de.EntityAspect.Delete());
      }
      targetEntity.EntityAspect.Delete();
      var sr0 = await em.SaveChanges();
      var deletedEntities = sr0.Entities;
      Assert.IsTrue(deletedEntities.Contains(targetEntity), "should contain target");
      if (expandPropName != null) {
        Assert.IsTrue(deletedEntities.Count == dependentEntities.Count + 1);
      }
      Assert.IsTrue(deletedEntities.All(de => de.EntityAspect.EntityState.IsDetached()), "should all be detached");

      // try to refetch deleted
      var r1 = await em.ExecuteQuery(targetKey.ToQuery<T>());
      Assert.IsTrue(r1.Count() == 0, "should not be able to find entity after delete");
      return r1;
    }

    private async Task<SaveResult> CreateAndSaveBillingDetail<T>(EntityManager em) where T:IBillingDetail, IEntity {
      var bd = em.CreateEntity<T>(EntityState.Detached);
      var ba = bd as IBankAccount;
      if (ba != null) {
        ba.Id = TestFns.GetNextInt();
        ba.CreatedAt = new DateTime();
        ba.Owner = "Scrooge McDuck";
        ba.Number = "999-999-9";
        ba.BankName = "Bank of Duckburg";
        ba.Swift = "RICHDUCK";
      } else {
        bd.Id = TestFns.GetNextInt();
        bd.CreatedAt = new DateTime();
        bd.Owner = "Richie Rich";
        bd.Number = "888-888-8";
      }
      em.AddEntity(bd);
      Assert.IsTrue(bd.MiscData == "asdf");
      SaveResult sr = null;
      try {
        sr = await em.SaveChanges();
        Assert.IsTrue(bd.EntityAspect.EntityState.IsUnchanged());
        
      } catch (Exception e) {
        var x = e;
        throw;
      }
      return sr;
    }

    

    //function createBillingDetailWithES5(typeName, baseTypeName, data) {
        
    //    var em = newEmX();
                
    //    var baseType = registerBaseBillingDetailWithES5(em, baseTypeName);
        

    //    var x = em.createEntity(typeName, data);
    //    ok(x.entityAspect.entityState === EntityState.Added);

    //    ok(x.entityType.isSubtypeOf(baseType), "is subtype of " + baseTypeName);

    //    var number = x.getProperty("number");
    //    ok(number === data.number);

    //    var miscData = x.getProperty("miscData");
    //    ok(miscData === "asdf", "miscData === asdf");

    //    var owner = x.getProperty("owner");
    //    ok(owner.length > 1, "has owner property");
    //    ok(owner === data.owner.toUpperCase(), "owner property is uppercase");

    //    var idAndOwner = x.getProperty("idAndOwner");
    //    ok(idAndOwner.length > 1, "has idAndOwner property");
    //    var id = x.getProperty("id");
    //    var owner = x.getProperty("owner");
    //    ok(idAndOwner == (id + ':' + owner), "idAndOwner property == id:owner");
    //}
    
    // var billingDetailData = {
    //    id: 456,
    //    createdAt: new Date(),
    //    owner: "Richie Rich",
    //    number: "888-888-8"
    //};

    //var bankAccountData = {
    //    id: 789,
    //    createdAt: new Date(),
    //    owner: "Scrooge McDuck",
    //    number: "999-999-9",
    //    bankName: "Bank of Duckburg",
    //    swift: "RICHDUCK"
    //};
  }
}

  
